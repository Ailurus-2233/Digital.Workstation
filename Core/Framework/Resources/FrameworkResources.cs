using DigitalWorkstation.Core.Resource;

namespace DigitalWorkstation.Core.Framework.Resources;

/// <summary>
///     所属模块的界面文案；中性资源为中文，en-US 为英文。
/// </summary>
public static class FrameworkResources
{
    /// <summary>
    ///     命令面板输入框的水印（ADR-0005 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)）
    /// </summary>
    public static string CommandPaletteWatermark => ResourceText.Get(typeof(FrameworkResources), nameof(CommandPaletteWatermark));

    /// <summary>
    ///     命令面板无匹配结果时的空态文案（ADR-0005 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)）
    /// </summary>
    public static string NoMatchingCommands => ResourceText.Get(typeof(FrameworkResources), nameof(NoMatchingCommands));

    /// <summary>
    ///     设置页"常规"分组的显示名（Framework 预置设置分组，ADR-0006 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md)）
    /// </summary>
    public static string SettingsGeneralGroupName => ResourceText.Get(typeof(FrameworkResources), nameof(SettingsGeneralGroupName));

    /// <summary>
    ///     设置项"语言"的显示名（Framework 预置，ADR-0006 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md)）
    /// </summary>
    public static string SettingsLanguageName => ResourceText.Get(typeof(FrameworkResources), nameof(SettingsLanguageName));

    /// <summary>
    ///     语言设置项成员 ZhCN 的显示名（键按「设置项名称键 + 成员名」约定生成，ADR-0006 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 8）
    /// </summary>
    public static string SettingsLanguageNameZhCN => ResourceText.Get(typeof(FrameworkResources), nameof(SettingsLanguageNameZhCN));

    /// <summary>
    ///     语言设置项成员 EnUS 的显示名（同上约定）
    /// </summary>
    public static string SettingsLanguageNameEnUS => ResourceText.Get(typeof(FrameworkResources), nameof(SettingsLanguageNameEnUS));
}
