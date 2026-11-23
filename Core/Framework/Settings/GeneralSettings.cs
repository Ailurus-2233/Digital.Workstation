using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.Core.Services.Network;
using DigitalWorkstation.Core.Framework.Resources;

namespace DigitalWorkstation.Core.Framework.Settings;

/// <summary>
///     Framework 预置的常规与网络代理设置。
///     声明锚点属性体不会被读取——读写一律经 ISettingsService，扫描只读 attribute 与属性类型
/// </summary>
[SettingGroup(GeneralSettings.GroupId, typeof(FrameworkResources), nameof(FrameworkResources.SettingsGeneralGroupName), Order = 0)]
[SettingGroup(ProxyGroupId, typeof(FrameworkResources), nameof(NetworkProxy))]
public static class GeneralSettings
{
    public const string GroupId = "framework.general";
    private const string ProxyGroupId = "framework.network-proxy";

    [SettingItem(ProxyGroupId, typeof(FrameworkResources), nameof(NetworkProxy), Id = ProxySettings.SettingId)]
    public static ProxySettings NetworkProxy => ProxySettings.Default;

    /// <summary>
    ///     语言设置项 Id（默认规则「声明类全名.属性名」），供启动序列等消费方读写
    /// </summary>
    public static readonly string LanguageSettingId = $"{typeof(GeneralSettings).FullName}.{nameof(Language)}";

    /// <summary>
    ///     界面语言：修改立即落盘、当前进程界面语言不变、下次启动生效
    /// </summary>
    [SettingItem(GroupId, typeof(FrameworkResources), nameof(FrameworkResources.SettingsLanguageName),
        DefaultValue = UiLanguage.ZhCN, RequiresRestart = true)]
    public static UiLanguage Language => UiLanguage.ZhCN;
}
