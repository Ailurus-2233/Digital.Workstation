using DigitalWorkstation.Core.Resource;

namespace DigitalWorkstation.Settings.Resources;

/// <summary>
///     所属模块的界面文案；中性资源为中文，en-US 为英文。
/// </summary>
public static class SettingsResources
{
    /// <summary>
    ///     设置页需重启设置项被修改后的项级标记文本（ADR-0006 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 7）
    /// </summary>
    public static string SettingsRestartPendingMark => ResourceText.Get(typeof(SettingsResources), nameof(SettingsRestartPendingMark));

    /// <summary>
    ///     设置页顶部「存在未生效的需重启修改」横幅文本（ADR-0006 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 7）
    /// </summary>
    public static string SettingsRestartBannerText => ResourceText.Get(typeof(SettingsResources), nameof(SettingsRestartBannerText));

    /// <summary>
    ///     设置页重启横幅上「立即重启」按钮的标题（ADR-0006 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 7）
    /// </summary>
    public static string SettingsRestartNowButtonTitle => ResourceText.Get(typeof(SettingsResources), nameof(SettingsRestartNowButtonTitle));
}
