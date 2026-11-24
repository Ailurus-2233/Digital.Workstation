namespace DigitalWorkstation.Core.Common.Events;

/// <summary>
///     请求回到启动时的主页：由文件菜单或命令面板发布，主窗口订阅。
///     展示主页，保留面板布局与已打开的标签。
/// </summary>
public class ReturnHomeEvent : PubSubEvent;
