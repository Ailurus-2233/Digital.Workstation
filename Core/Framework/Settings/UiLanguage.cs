using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;

namespace DigitalWorkstation.Core.Framework.Settings;

/// <summary>
///     界面语言（Framework 预置「常规/语言」设置项的值类型，ADR-0006 决策 9）。
///     成员的 <see cref="JsonStringEnumMemberNameAttribute" /> 值即对应 CultureInfo 名称——
///     settings.json 落盘值与区域性名称同源（经 JsonStringEnumConverter 序列化）
/// </summary>
public enum UiLanguage
{
    /// <summary>
    ///     中文（简体），默认语言
    /// </summary>
    [JsonStringEnumMemberName("zh-CN")]
    ZhCN,

    /// <summary>
    ///     English (US)
    /// </summary>
    [JsonStringEnumMemberName("en-US")]
    EnUS
}

/// <summary>
///     <see cref="UiLanguage" /> 到 CultureInfo 的映射：成员的 JsonStringEnumMemberName 值即区域性名称
/// </summary>
public static class UiLanguageExtensions
{
    public static CultureInfo ToCultureInfo(this UiLanguage language)
    {
        var member = typeof(UiLanguage).GetMember(language.ToString())[0];
        var name = member.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name ?? language.ToString();
        return CultureInfo.GetCultureInfo(name);
    }
}
