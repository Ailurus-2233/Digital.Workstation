namespace DigitalWorkstation.Core.Models.Events;

/// <summary>
///     请求翻转指定面板/区域的可见性；负载为目标区域（<see cref="TogglePanelTarget" />）。
///     由菜单栏与快速工具栏的面板显隐切换项发布，主窗口订阅后与快捷键走同一状态转换
/// </summary>
public class TogglePanelVisibilityEvent : PubSubEvent<TogglePanelTarget>;
