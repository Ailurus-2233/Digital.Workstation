namespace DigitalWorkstation.Core.Models.Events;

/// <summary>
///     模块加载失败负载；启动台显示错误详情后等待用户决策
/// </summary>
/// <param name="ModuleName">失败的模块名</param>
/// <param name="ModuleIndex">失败模块序号（从 1 开始）</param>
/// <param name="ModuleCount">模块总数</param>
/// <param name="ErrorMessage">错误详情</param>
public record ModuleLoadFailure(string ModuleName, int ModuleIndex, int ModuleCount, string ErrorMessage);
