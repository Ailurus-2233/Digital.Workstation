namespace DigitalWorkstation.Core.Common.Events;

/// <summary>
///     由 SideBar 内交互与 shell 的"设置"导航按钮发布；
///     ActivityBar 导航切换不发布本事件，MainContent 保持不变
/// </summary>
public class OpenMainViewEvent : PubSubEvent<string>;
