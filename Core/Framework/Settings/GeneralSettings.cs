using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.Core.Framework.Resources;

namespace DigitalWorkstation.Core.Framework.Settings;

/// <summary>
///     Framework 预置设置项（ADR-0006 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 9）：「常规 / 语言」（zh-CN / en-US，需重启）。
///     声明锚点属性体不会被读取——读写一律经 ISettingsService，扫描只读 attribute 与属性类型
/// </summary>
[SettingGroup(GeneralSettings.GroupId, typeof(FrameworkResources), nameof(FrameworkResources.SettingsGeneralGroupName), Order = 0)]
public static class GeneralSettings
{
    public const string GroupId = "framework.general";

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
