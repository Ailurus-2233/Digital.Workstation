namespace DigitalWorkstation.Core.Models.Events;

/// <summary>
///     请求 MainContent 打开指定主视图；负载为主视图 Id（IMainViewContribution.Id）。
///     仅由 SideBar 内交互发布；ActivityBar 导航切换不发布本事件，MainContent 保持不变
/// </summary>
public class OpenMainViewEvent : PubSubEvent<string>;
