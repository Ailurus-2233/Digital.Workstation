using Avalonia.Controls;
using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.Framework;
using DigitalWorkstation.Core.Framework.Commands;
using DigitalWorkstation.Core.Framework.Contributions;
using DigitalWorkstation.Core.Framework.Menus;
using DigitalWorkstation.DashBoard;
using DigitalWorkstation.DashBoard.Views.Windows;
using DigitalWorkstation.Settings;
using DigitalWorkstation.Workstation.Contributions;
using DigitalWorkstation.Workstation.Views;

namespace DigitalWorkstation.Workstation;

public class WorkstationApplication : FrameworkApplication<MainWindow>
{
    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        moduleCatalog.AddModule<DashBoardModule>();
        moduleCatalog.AddModule<SettingsModule>();
    }
    
    protected override void RegisterCustomService(IContainerRegistry containerRegistry)
    {
        // 工具视图（ADR-0002，attribute 扫描）：当前本程序集无 [ToolView] 标注类（原四个演示占位视图已删除），
        // 扫描注册为空，保留该行以覆盖将来新增；标注 [ToolView] 的 View 会同时注册进容器
        containerRegistry.RegisterToolViews(typeof(WorkstationApplication).Assembly);
        // shell 内置空状态页：MainContent 尚无活动视图时显示，不依赖任何模块
        containerRegistry.Register<EmptyStateView>();
        // shell 预置菜单：文件>退出、帮助>关于；视图>三面板显隐切换 + 四档面板对齐（attribute 扫描注册，ADR-0001）
        containerRegistry.RegisterMenus(typeof(WorkstationApplication).Assembly);
        // shell 预置命令：三面板显隐切换 + 重置布局（attribute 扫描注册，ADR-0005）
        containerRegistry.RegisterCommands(typeof(WorkstationApplication).Assembly);
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