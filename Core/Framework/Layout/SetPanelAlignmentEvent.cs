namespace DigitalWorkstation.Core.Framework.Layout;

/// <summary>
///     请求切换窗口布局（面板对齐）档位；负载为目标档位（<see cref="PanelAlignment" />）。
///     由视图菜单的面板对齐项发布，主窗口订阅后写入 PanelAlignment 依赖属性。
///     契约放本模块而非 Core/Models：负载类型 PanelAlignment 定义于此，Models 引用 Framework 会成环
/// </summary>
public class SetPanelAlignmentEvent : PubSubEvent<PanelAlignment>;
