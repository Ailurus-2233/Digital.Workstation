using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.Framework.Commands;
using DigitalWorkstation.Core.Framework.Contributions;
using DigitalWorkstation.Core.Framework.Menus;
using DigitalWorkstation.DashBoard.Views;

namespace DigitalWorkstation.DashBoard;

public class DashBoardModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 工具视图（ADR-0002，attribute 扫描）：ActivityBar"启动台"、BottomPanel"任务"；
        // 标注 [ToolView] 的 View 同时注册进容器
        containerRegistry.RegisterToolViews(typeof(DashBoardModule).Assembly);
        containerRegistry.RegisterSingleton<IMainViewContribution, DashBoardOverviewMainView>();
        containerRegistry.RegisterSingleton<IMainViewContribution, DashBoardRecentMainView>();
        containerRegistry.RegisterMenus(typeof(DashBoardModule).Assembly);
        // 命令（ADR-0005，attribute 扫描）：打开启动台
        containerRegistry.RegisterCommands(typeof(DashBoardModule).Assembly);
        containerRegistry.RegisterSingleton<IStatusBarItemContribution, DashBoardStatusBarItem>();
        containerRegistry.Register<DashBoardOverviewView>();
        containerRegistry.Register<DashBoardRecentView>();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 启动台窗口由 shell 启动序列在模块加载前显示（ADR-0004），模块自身不再开窗
    }
}
