using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using DigitalWorkstation.Core.Abstractions.WindowManager;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Framework.Shell;
using DigitalWorkstation.Core.Framework.WindowManager;
using DigitalWorkstation.Core.Models.Events;
using DigitalWorkstation.Core.UIPackage;
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
    ///     框架初始化完成后执行启动序列：初始化核心服务 → 逐模块异步加载 → 就绪后显示主窗口（ADR-0004）。
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
    ///     抑制 Prism 同步 InitializeModules 一次性加载；模块改由启动序列逐模块异步加载（ADR-0004）
    /// </summary>
    protected override void InitializeModules()
    {
    }

    /// <summary>
    ///     供子类提供启动台窗口；模块加载进度与失败决策均经启动台呈现（ADR-0004）
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

            // 阶段 2：逐模块异步加载；加载移出 UI 线程，启动台进度不被阻塞
            var moduleManager = Container.Resolve<IModuleManager>();
            var modules = moduleCatalog.Modules.ToList();
            var total = modules.Count;
            for (var i = 0; i < total; i++)
            {
                var module = modules[i];
                progressEvent.Publish(new StartupProgress(StartupPhase.LoadingModules, module.ModuleName, i + 1, total));
                try
                {
                    await Task.Run(() => moduleManager.LoadModule(module.ModuleName));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"模块 {module.ModuleName} 加载失败");
                    eventAggregator.GetEvent<ModuleLoadFailedEvent>().Publish(
                        new ModuleLoadFailure(module.ModuleName, i + 1, total, ex.Message));
                    if (!await WaitForFailureActionAsync(eventAggregator))
                    {
                        (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
                        return;
                    }
                }
            }

            // 阶段 3：就绪——启动台关闭、工作区显示
            progressEvent.Publish(new StartupProgress(StartupPhase.Ready, null, total, total));
            ShowMainWindow();
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "启动序列执行失败");
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
        containerRegistry.RegisterSingleton<ShellContributionCollector>();
        
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
            var viewAssemblyName = viewType.GetTypeInfo().Assembly.FullName;

            if (string.IsNullOrEmpty(viewName) || string.IsNullOrEmpty(viewAssemblyName))
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

            var fullViewModelName = $"{viewModelName}, {viewAssemblyName}";

            return Type.GetType(fullViewModelName);
        });

        // 也可以为特定 View 设置特定 ViewModel
        // ViewModelLocationProvider.Register<SpecialView, SpecialViewModel>();
    }
}