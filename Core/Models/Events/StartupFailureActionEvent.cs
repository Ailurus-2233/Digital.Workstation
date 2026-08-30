namespace DigitalWorkstation.Core.Models.Events;

/// <summary>
///     模块加载失败后的用户决策事件；仅由启动台"继续/退出"按钮发布，
///     启动序列等待该决策以决定跳过该模块还是终止应用
/// </summary>
public class StartupFailureActionEvent : PubSubEvent<StartupFailureAction>;
