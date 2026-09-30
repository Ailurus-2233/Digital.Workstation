using DigitalWorkstation.Core.Abstractions.Layout;

namespace DigitalWorkstation.Core.Common.Events;

/// <summary>
///     请求切换窗口布局（面板对齐）档位；负载为目标档位（<see cref="PanelAlignment" />）。
///     由视图菜单的面板对齐项发布，主窗口订阅后写入 PanelAlignment 依赖属性。
/// </summary>
public class SetPanelAlignmentEvent : PubSubEvent<PanelAlignment>;
