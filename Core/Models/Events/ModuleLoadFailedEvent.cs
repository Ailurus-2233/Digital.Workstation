namespace DigitalWorkstation.Core.Models.Events;

/// <summary>
///     模块加载失败事件：启动序列逐模块加载时捕获异常后发布（ADR-0004）。
///     启动台订阅后显示错误详情，并提供"继续（跳过该模块）/退出"选择
/// </summary>
public class ModuleLoadFailedEvent : PubSubEvent<ModuleLoadFailure>;
