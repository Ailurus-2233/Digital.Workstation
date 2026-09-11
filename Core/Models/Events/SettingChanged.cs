namespace DigitalWorkstation.Core.Models.Events;

/// <summary>
///     设置项变更负载（ADR-0006 决策 3）：设置项 Id 与新值。
///     发布方：Framework 的 SettingsService（Set 时）；订阅方：设置页与需重启 UX 等消费方
/// </summary>
/// <param name="SettingId">设置项 Id（SettingItemContribution.Id）</param>
/// <param name="NewValue">新值（装箱后的设置值，类型由设置项声明的 ValueType 决定）</param>
public record SettingChanged(string SettingId, object? NewValue);
