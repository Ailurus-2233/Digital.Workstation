using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.Framework.Contributions;

namespace DigitalWorkstation.DashBoard;

public class DashBoardModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 工具视图（ADR-0002，attribute 扫描）：当前程序集无 [ToolView] 标注类（原"启动台"导航视图与
        // "任务"演示 tab 已删除），扫描注册为空；保留该行以覆盖将来新增
        containerRegistry.RegisterToolViews(typeof(DashBoardModule).Assembly);
        containerRegistry.RegisterSingleton<IStatusBarItemContribution, DashBoardStatusBarItem>();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 启动台窗口由 shell 启动序列在模块加载前显示（ADR-0004），模块自身不再开窗
    }
}
