namespace DigitalWorkstation.Core.Models.Events;

/// <summary>
///     设置项变更事件（ADR-0006 决策 3）：SettingsService.Set 更新内存后广播。
///     发布方：Framework 的 SettingsService；订阅方：设置页、需重启 UX 等需响应设置变更的消费方
/// </summary>
public class SettingChangedEvent : PubSubEvent<SettingChanged>;
