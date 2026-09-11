namespace DigitalWorkstation.Core.Models.Events;

/// <summary>
///     由 SideBar 内交互与 shell 的"设置"导航按钮（ADR-0006 决策 6）发布；
///     ActivityBar 导航切换不发布本事件，MainContent 保持不变
/// </summary>
public class OpenMainViewEvent : PubSubEvent<string>;
