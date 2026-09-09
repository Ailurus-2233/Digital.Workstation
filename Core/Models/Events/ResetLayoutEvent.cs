namespace DigitalWorkstation.Core.Models.Events;

/// <summary>
///     请求重置布局（ADR-0002）：删除持久化布局配置并按 attribute 默认重建 shell 布局。
///     由视图菜单的"重置布局"项发布，主窗口订阅后重建 State；无负载
/// </summary>
public class ResetLayoutEvent : PubSubEvent;
