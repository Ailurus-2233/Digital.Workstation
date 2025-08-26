using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using DigitalWorkstation.Common;
using DigitalWorkstation.Common.Events;
using DigitalWorkstation.Framework.WindowManager;
using DigitalWorkstation.WindowManager;
using Prism.DryIoc;

namespace DigitalWorkstation.Framework;

public abstract class FrameworkApplication<TWindow> : PrismApplication where TWindow : Window
{
    private IEventAggregator? _eventAggregator;
    private IWindowManager? _windowManager;

    /// <summary>
    ///     覆盖原有的方法，框架初始化完成时不进行任何操作
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
    }

    /// <summary>
    ///     覆盖原有的方法，框架初始化时订阅显示主窗口事件
    /// </summary>
    protected override void OnInitialized()
    {
        
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
        containerRegistry.RegisterSingleton<IWindowManager, FrameworkWindowManager>();
        
        ResolveFrameworkServices();
    }
    
    private void ResolveFrameworkServices()
    {
        // 解析框架所需的服务
        _eventAggregator = Container.Resolve<IEventAggregator>();
        _windowManager = Container.Resolve<IWindowManager>();
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

    protected override void InitializeShell(AvaloniaObject shell)
    {
        base.InitializeShell(shell);
        
        // 在完成 MainWindow 初始化后，执行 Framework 服务相关的初始化
        _eventAggregator?.GetEvent<ShowMainWindowEvent>().Subscribe(ShowMainWindow, ThreadOption.UIThread, true);
        _windowManager?.HandleMainWindow();
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