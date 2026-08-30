namespace DigitalWorkstation.Core.Models.Events;

/// <summary>
///     启动阶段（ADR-0004）：Launcher 引导发生在 Avalonia 启动前，不在进度覆盖范围内
/// </summary>
public enum StartupPhase
{
    /// <summary>
    ///     初始化核心服务（窗口管理器登记主窗口、模块目录校验）
    /// </summary>
    CoreServices,

    /// <summary>
    ///     逐模块加载（显示当前模块名与 i/N 进度）
    /// </summary>
    LoadingModules,

    /// <summary>
    ///     全部模块就绪，启动台即将关闭、工作区显示
    /// </summary>
    Ready
}
