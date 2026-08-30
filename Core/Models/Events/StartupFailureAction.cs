namespace DigitalWorkstation.Core.Models.Events;

/// <summary>
///     模块加载失败后的用户决策
/// </summary>
public enum StartupFailureAction
{
    /// <summary>
    ///     继续：跳过失败模块，加载其余模块并进入工作区
    /// </summary>
    Continue,

    /// <summary>
    ///     退出：终止应用
    /// </summary>
    Exit
}
