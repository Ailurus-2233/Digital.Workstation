using Avalonia.Controls;
using DigitalWorkstation.Core.Abstractions.Shell;
using DigitalWorkstation.Core.Framework;
using DigitalWorkstation.Core.Models.Events;
using DigitalWorkstation.DashBoard;
using DigitalWorkstation.DashBoard.Views.Windows;
using DigitalWorkstation.Workstation.Shell;
using DigitalWorkstation.Workstation.Views;

namespace DigitalWorkstation.Workstation;

public class WorkstationApplication : FrameworkApplication<MainWindow>
{
    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        moduleCatalog.AddModule<DashBoardModule>();
    }
    
    protected override void RegisterCustomService(IContainerRegistry containerRegistry)
    {
        // shell 预置的"设置"导航项（ActivityBar 底部）
        containerRegistry.RegisterSingleton<INavigationItemContribution, SettingsNavigationItem>();
        // shell 内置空状态页：MainContent 尚无活动视图时显示，不依赖任何模块
        containerRegistry.Register<EmptyStateView>();
        // shell 预置的演示面板 tab：AuxiliaryPanel"属性/大纲"、BottomPanel"输出/日志"
        containerRegistry.RegisterSingleton<IPanelTabContribution, PropertiesPanelTab>();
        containerRegistry.RegisterSingleton<IPanelTabContribution, OutlinePanelTab>();
        containerRegistry.RegisterSingleton<IPanelTabContribution, OutputPanelTab>();
        containerRegistry.RegisterSingleton<IPanelTabContribution, LogPanelTab>();
        containerRegistry.Register<PropertiesView>();
        containerRegistry.Register<OutlineView>();
        containerRegistry.Register<OutputView>();
        containerRegistry.Register<LogView>();
        // shell 预置菜单项：文件>退出、帮助>关于；视图>三面板显隐切换（同一元数据同时贡献给快速工具栏）
        containerRegistry.RegisterSingleton<IMenuItemContribution, ExitMenuItem>();
        containerRegistry.RegisterSingleton<IMenuItemContribution, AboutMenuItem>();
        foreach (var target in new[] { TogglePanelTarget.SideBar, TogglePanelTarget.BottomPanel, TogglePanelTarget.AuxiliaryPanel })
        {
            containerRegistry.RegisterSingleton(typeof(IMenuItemContribution),
                provider => new TogglePanelContribution(provider.Resolve<IEventAggregator>(), target));
            containerRegistry.RegisterSingleton(typeof(IToolBarItemContribution),
                provider => new TogglePanelContribution(provider.Resolve<IEventAggregator>(), target));
        }
        // shell 预置状态栏项"就绪"
        containerRegistry.RegisterSingleton<IStatusBarItemContribution, ReadyStatusBarItem>();
        // "关于"对话框：经窗口管理器按需解析
        containerRegistry.Register<AboutWindow>();
    }

    /// <summary>
    ///     启动台：DashBoard 模块的进度窗（ADR-0004）；模块逐模块加载前由 shell 直接解析显示
    /// </summary>
    protected override Window CreateSplashWindow()
    {
        return Container.Resolve<DashBoardWindow>();
    }
}