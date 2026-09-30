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
    public static string GeneralSettingsTabTitle => ResourceText.Get(typeof(SettingsResources), nameof(GeneralSettingsTabTitle));
    public static string PluginsTitle => ResourceText.Get(typeof(SettingsResources), nameof(PluginsTitle));
    public static string PluginsDescription => ResourceText.Get(typeof(SettingsResources), nameof(PluginsDescription));
    public static string PluginsRestartMessage => ResourceText.Get(typeof(SettingsResources), nameof(PluginsRestartMessage));
    public static string PluginsRestartNow => ResourceText.Get(typeof(SettingsResources), nameof(PluginsRestartNow));
    public static string PluginsEmpty => ResourceText.Get(typeof(SettingsResources), nameof(PluginsEmpty));
    public static string PluginLoadedStatus => ResourceText.Get(typeof(SettingsResources), nameof(PluginLoadedStatus));
    public static string PluginDisabledStatus => ResourceText.Get(typeof(SettingsResources), nameof(PluginDisabledStatus));
    public static string PluginFailedStatus => ResourceText.Get(typeof(SettingsResources), nameof(PluginFailedStatus));
    public static string PluginPendingRestart => ResourceText.Get(typeof(SettingsResources), nameof(PluginPendingRestart));
    public static string PluginLoadAtStartup => ResourceText.Get(typeof(SettingsResources), nameof(PluginLoadAtStartup));
    public static string PluginEnabled => ResourceText.Get(typeof(SettingsResources), nameof(PluginEnabled));
    public static string PluginDelete => ResourceText.Get(typeof(SettingsResources), nameof(PluginDelete));
    public static string PluginRemovalPending => ResourceText.Get(typeof(SettingsResources), nameof(PluginRemovalPending));
}
