namespace DigitalWorkstation.Core.Models.Events;

/// <summary>
///     启动进度事件：覆盖核心服务初始化与逐模块加载（ADR-0004）。
///     由框架启动序列发布，启动台订阅后显示阶段名、当前模块名与 i/N
/// </summary>
public class StartupProgressEvent : PubSubEvent<StartupProgress>;
