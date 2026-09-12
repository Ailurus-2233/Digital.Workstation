namespace DigitalWorkstation.Core.Models.Events;

/// <summary>
///     请求回到启动时的主页：由文件菜单或命令面板发布，主窗口订阅。
///     仅清除活动主视图并恢复主页内容，保留面板布局与主视图实例缓存；无负载
/// </summary>
public class ReturnHomeEvent : PubSubEvent;
