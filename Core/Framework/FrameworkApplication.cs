using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using DigitalWorkstation.Core.Abstractions.Settings;
using Avalonia.Styling;
using DigitalWorkstation.Core.Abstractions.WindowManager;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Framework.Contributions;
using DigitalWorkstation.Core.Framework.Layout;
using DigitalWorkstation.Core.Framework.Persistence;
using DigitalWorkstation.Core.Framework.Plugins;
using DigitalWorkstation.Core.Framework.Settings;
using DigitalWorkstation.Core.Framework.WindowManager;
using DigitalWorkstation.Core.Models.Events;
using DigitalWorkstation.Core.UIPackage;
using DigitalWorkstation.Core.Resource;
using Prism.DryIoc;

namespace DigitalWorkstation.Core.Framework;

public abstract class FrameworkApplication<TWindow> : PrismApplication where TWindow : Window
{
    private IEventAggregator? _eventAggregator;
    private IMainWindowManager? _windowManager;

    public override void Initialize()
    {
        // 固定 Dark：当前设计目标为 VS Code Dark+ 单一色调，未做亮色适配
        RequestedThemeVariant = ThemeVariant.Dark;
        Styles.AddRange(new WorkstationTheme());
        // VS Code Dark+ 色调：覆盖 Semi 语义色键并提供 chrome 专属色键
        VSCodePalette.ApplyTo(Resources);
        base.Initialize();
    }

    /// <summary>
    ///     框架初始化完成后执行启动序列：初始化核心服务 → 逐模块异步加载 → 就绪后显示主窗口（ADR-0004 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0004-startup-sequence.md)）。
    ///     不调用 base：base 会把尚未完成模块加载的 MainWindow 直接设为桌面生命周期主窗口
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        _ = RunStartupSequenceAsync();
    }

    /// <summary>
    ///     覆盖原有的方法，阻止 base 在框架初始化阶段直接显示 MainWindow；
    ///     主窗口由启动序列在全部模块就绪后显示
    /// </summary>
    protected override void OnInitialized()
    {
    }

    /// <summary>
    ///     抑制 Prism 同步 InitializeModules 一次性加载；模块改由启动序列逐模块异步加载（ADR-0004 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0004-startup-sequence.md)）
    /// </summary>
    protected override void InitializeModules()
    {
    }

    /// <summary>
    ///     供子类提供启动台窗口；模块加载进度与失败决策均经启动台呈现（ADR-0004 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0004-startup-sequence.md)）
    /// </summary>
    protected abstract Window CreateSplashWindow();

    /// <summary>
    ///     启动序列：初始化核心服务 → 逐模块异步加载并发布进度 → 就绪后自动收尾（关闭启动台、显示工作区）。
    ///     单模块失败时发布失败事件并等待启动台"继续（跳过该模块）/退出"决策
    /// </summary>
    private async Task RunStartupSequenceAsync()
    {
        try
        {
            var eventAggregator = _eventAggregator!;
            var progressEvent = eventAggregator.GetEvent<StartupProgressEvent>();

            // 阶段 1：初始化核心服务——登记主窗口、显示启动台、校验模块目录
            _windowManager!.HandleMainWindow();
            Container.Resolve<IWindowManager>().ShowWindow(CreateSplashWindow());
            progressEvent.Publish(new StartupProgress(StartupPhase.CoreServices, null, 0, 0));
            var moduleCatalog = Container.Resolve<IModuleCatalog>();
            moduleCatalog.Initialize();

            // 内置模块仍由 Prism 排序；插件发现不执行入口构造或注册。
            var moduleManager = Container.Resolve<IModuleManager>();
            var modules = moduleCatalog.CompleteListWithDependencies(moduleCatalog.Modules).ToList();
            var builtInNames = modules.Select(module => module.ModuleName).ToHashSet(StringComparer.Ordinal);
            var sharedModules = modules.Select(module => Type.GetType(module.ModuleType, throwOnError: true)!.Assembly)
                .Append(typeof(TWindow).Assembly).Distinct().ToArray();
            var discoveredPlugins = await Task.Run(() => PluginDiscovery.Discover(sharedModules));
            var plugins = new List<(PluginDescriptor Descriptor, ModuleInfo? Info)>();
            var names = new HashSet<string>(builtInNames, StringComparer.Ordinal);
            foreach (var discovered in discoveredPlugins)
            {
                var plugin = discovered;
                if (plugin.Error is null && !names.Add(plugin.Name))
                    plugin = plugin with { Error = new InvalidOperationException($"Duplicate module or plugin name: {plugin.Name}.") };
                if (plugin.Error is null && plugin.Dependencies.Any(name => !builtInNames.Contains(name)))
                    plugin = plugin with { Error = new InvalidOperationException("Plugins may only declare dependencies on built-in modules.") };

                ModuleInfo? info = null;
                if (plugin.Error is null && plugin.ModuleType is not null)
                {
                    info = new ModuleInfo
                    {
                        ModuleName = plugin.Name,
                        ModuleType = plugin.ModuleType.AssemblyQualifiedName!,
                        InitializationMode = InitializationMode.OnDemand
                    };
                    foreach (var dependency in plugin.Dependencies)
                        info.DependsOn.Add(dependency);
                    moduleCatalog.AddModule(info);
                }
                plugins.Add((plugin, info));
            }

            var contributions = Container.Resolve<ShellContributionCatalog>();
            var availableModules = new HashSet<string>(StringComparer.Ordinal);
            var total = modules.Count + plugins.Count;
            var current = 0;

            // 两种入口共用进度、贡献批次和失败决策，始终先完成全部内置模块。
            async Task<bool> PrepareItemAsync(string name, IEnumerable<string> dependencies, Func<Task> initialize)
            {
                progressEvent.Publish(new StartupProgress(StartupPhase.LoadingModules, name, ++current, total));
                using var batch = ShellContributionCatalog.BeginBatch(name);
                try
                {
                    var unavailable = dependencies.FirstOrDefault(dependency => !availableModules.Contains(dependency));
                    if (unavailable is not null)
                        throw new InvalidOperationException($"Required module {unavailable} is unavailable.");
                    await initialize();
                    contributions.Prepare(batch);
                    availableModules.Add(name);
                    return true;
                }
                catch (Exception ex)
                {
                    batch.Reject();
                    Logger.Error(ex, $"Failed to prepare module or plugin {name}");
                    var failureAction = WaitForFailureActionAsync(eventAggregator);
                    eventAggregator.GetEvent<ModuleLoadFailedEvent>().Publish(
                        new ModuleLoadFailure(name, current, total, ex.Message));
                    if (await failureAction) return true;
                    (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
                    return false;
                }
            }

            foreach (var module in modules)
            {
                if (!await PrepareItemAsync(module.ModuleName, module.DependsOn, async () =>
                    {
                        await Task.Run(() => moduleManager.LoadModule(module.ModuleName));
                        if (module.State != ModuleState.Initialized)
                            throw new InvalidOperationException($"Module {module.ModuleName} did not finish initialization.");
                    })) return;
            }

            foreach (var (plugin, info) in plugins)
            {
                if (!await PrepareItemAsync(plugin.Name, plugin.Error is null ? plugin.Dependencies : [], async () =>
                    {
                        if (plugin.Error is not null) throw plugin.Error;
                        var type = plugin.ModuleType ?? throw new InvalidOperationException("Plugin entry type is missing.");
                        Logger.Information($"Loading plugin {plugin.Name} from {plugin.AssemblyPath}");
                        if (info is not null) info.State = ModuleState.Initializing;
                        await Task.Run(() =>
                        {
                            // 使用发现的 Type 保留插件上下文，避免 Prism 按程序集名重新查找。
                            IoC.Registry.RegisterSingleton(type, type);
                            var instance = (IModule)Container.Resolve(type);
                            instance.RegisterTypes(IoC.Registry);
                            instance.OnInitialized(Container);
                        });
                    })) return;
                if (info is not null)
                    info.State = availableModules.Contains(plugin.Name) ? ModuleState.Initialized : ModuleState.NotStarted;
            }

            // 阶段 3：就绪——启动台关闭、工作区显示
            contributions.PrepareUnowned();
            PrepareShell();
            progressEvent.Publish(new StartupProgress(StartupPhase.Ready, null, total, total));
            ShowMainWindow();
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Startup sequence failed");
            (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
        }
    }

    /// <summary>
    ///     等待启动台发布"继续/退出"决策；返回 true 表示继续（跳过失败模块）
    /// </summary>
    private static async Task<bool> WaitForFailureActionAsync(IEventAggregator eventAggregator)
    {
        var actionEvent = eventAggregator.GetEvent<StartupFailureActionEvent>();
        var completion = new TaskCompletionSource<StartupFailureAction>(TaskCreationOptions.RunContinuationsAsynchronously);
        var token = actionEvent.Subscribe(action => completion.TrySetResult(action));
        var action = await completion.Task;
        actionEvent.Unsubscribe(token);
        return action == StartupFailureAction.Continue;
    }

    /// <summary>
    ///     全部可用贡献准备完毕后、发布 Ready 前，由宿主完成布局、菜单和手势接线。
    /// </summary>
    protected virtual void PrepareShell() { }

    private void ShowMainWindow()
    {
        if (MainWindow is not Window window ||
            ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime) return;
        lifetime.MainWindow = window;
        _windowManager?.ShowMainWindow();
        _windowManager?.CloseWindowsExceptMain();
    }

    /// <summary>
    ///     注册框架所需要的服务
    /// </summary>
    /// <param name="containerRegistry">
    ///     Prism 容器注册接口
    /// </param>
    private void RegisterFrameworkServices(IContainerRegistry containerRegistry)
    {
        IoC.Initialize(containerRegistry, Container);
        
        // 注册窗口管理
        var windowManager = new FrameworkWindowManager();
        containerRegistry.RegisterSingleton<IMainWindowManager>(() => windowManager);
        containerRegistry.RegisterSingleton<IWindowManager>(() => windowManager);
        
        // 注册 shell 贡献收集器
        containerRegistry.RegisterSingleton<ShellContributionCatalog>();
        containerRegistry.RegisterSingleton<SettingCatalog>();
        containerRegistry.RegisterSingleton<ShellContributionCollector>();

        // 在真正退出时统一保存并释放计时器；关闭被取消时继续保留防抖保存能力。
        var persistence = new ConfigurationPersistence();
        containerRegistry.RegisterInstance(persistence);
        if (ApplicationLifetime is IControlledApplicationLifetime lifetime)
        {
            lifetime.Exit += (_, _) => persistence.Dispose();
        }

        // 注册布局持久化服务（ADR-0002 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）：layout.json 读/防抖写/删，机制在 Framework、接线在 shell 模块
        containerRegistry.RegisterSingleton<LayoutPersistence>();

        // 注册设置服务（ADR-0006 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 3/4）：与窗口管理器同型——显式构造实例并以工厂注册，
        // 以便启动时一次性 Load 入内存的时机明确
        var settingsService = new SettingsService(Container.Resolve<IEventAggregator>(), Container.Resolve<SettingCatalog>(), persistence);
        settingsService.Load();
        containerRegistry.RegisterSingleton<ISettingsService>(() => settingsService);

        // 注册 Framework 自身设置项（常规/语言，ADR-0006 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 9），模块设置项由各自 RegisterTypes 注册
        containerRegistry.RegisterSettings(typeof(SettingsService).Assembly);

        ApplyLanguageSetting(settingsService);

        ResolveFrameworkServices();
    }
    
    private void ResolveFrameworkServices()
    {
        // 解析框架所需的服务
        _eventAggregator = Container.Resolve<IEventAggregator>();
        _windowManager = Container.Resolve<IMainWindowManager>();
    }

    /// <summary>
    ///     重写注册服务方法，注册框架所需的服务
    ///     注意：子类不需要重写此方法
    /// </summary>
    /// <param name="containerRegistry">
    ///     Prism 容器注册接口
    /// </param>
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        RegisterFrameworkServices(containerRegistry);
        RegisterCustomService(containerRegistry);
    }

    /// <summary>
    ///     按已存语言设置应用 UI 区域性（ADR-0006 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 7：下次启动由消费方读取生效）。
    ///     必须先于一切模块 RegisterTypes——菜单/工具视图标题在注册扫描时经 ResourceText.Get 一次性解析；
    ///     未存值时回退声明默认值（zh-CN），与操作系统区域性无关
    /// </summary>
    private void ApplyLanguageSetting(ISettingsService settings)
    {
        var culture = settings.Get<UiLanguage>(GeneralSettings.LanguageSettingId).ToCultureInfo();
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        Name = SharedResources.ProductName;
    }

    /// <summary>
    ///     供子类重写以注册自定义服务
    /// </summary>
    /// <param name="containerRegistry">
    ///     Prism 容器注册接口
    /// </param>
    protected virtual void RegisterCustomService(IContainerRegistry containerRegistry)
    {
        // 供子类重写以注册自定义服务
    }

    /// <summary>
    ///     创建 Shell, 使用泛型参数指定主窗口类型
    /// </summary>
    /// <returns></returns>
    protected override AvaloniaObject CreateShell()
    {
        return Container.Resolve<TWindow>();
    }

    /// <summary>
    ///     自动化 ViewModel 定位器，在使用容器初始化 view 时，会自动将 ViewModel 与 View 关联
    ///     当前自动关联方案：
    ///     **/Views/*View.xaml -> **/ViewModels/*ViewModel.cs
    ///     **/Views/Windows/*Window.xaml -> **/ViewModels/Windows/*WindowViewModel.cs
    ///     **/Views/Pages/*Page.xaml -> **/ViewModels/Pages/*PageViewModel.cs
    ///     在 View.xaml 中 使用 mvvm:ViewModelLocator.AutoWireViewModel="True" 来启用自动关联
    /// </summary>
    protected override void ConfigureViewModelLocator()
    {
        base.ConfigureViewModelLocator();

        ViewModelLocationProvider.SetDefaultViewTypeToViewModelTypeResolver(viewType =>
        {
            var viewName = viewType.FullName;
            var viewAssembly = viewType.Assembly;

            if (string.IsNullOrEmpty(viewName))
            {
                return null;
            }

            var viewModelName = viewName.Replace("Views", "ViewModels");
            if (viewModelName.EndsWith("Window") || viewModelName.EndsWith("Page"))
            {
                viewModelName += "ViewModel";
            }

            if (viewModelName.EndsWith("View"))
            {
                viewModelName += "Model";
            }

            return viewAssembly.GetType(viewModelName);
        });

        // 也可以为特定 View 设置特定 ViewModel
        // ViewModelLocationProvider.Register<SpecialView, SpecialViewModel>();
    }
}