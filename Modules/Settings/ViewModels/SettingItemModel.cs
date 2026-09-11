using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.Core.Resource;

namespace DigitalWorkstation.Settings.ViewModels;

/// <summary>
///     设置页右侧编辑区的设置项模型基类：名称与读写通路。
///     读写经反射调用 <see cref="ISettingsService" /> 的泛型方法——设置页是类型擦除消费方，
///     具体值类型只在声明（<see cref="SettingItemContribution.ValueType" />）里
/// </summary>
public abstract class SettingItemModel(SettingItemContribution contribution, ISettingsService settings)
    : ObservableObject
{
    private static readonly MethodInfo GetMethod =
        typeof(ISettingsService).GetMethod(nameof(ISettingsService.Get))!;

    private static readonly MethodInfo SetMethod =
        typeof(ISettingsService).GetMethod(nameof(ISettingsService.Set))!;

    /// <summary>
    ///     设置项 Id（ISettingsService 读写的依据）
    /// </summary>
    public string Id { get; } = contribution.Id;

    /// <summary>
    ///     设置项显示名（已解析）
    /// </summary>
    public string Name { get; } = Language.Get(contribution.Name);

    /// <summary>
    ///     名称资源键：枚举成员显示名按「本键 + 成员名」约定派生（ADR-0006 决策 8）
    /// </summary>
    public string NameKey { get; } = contribution.Name;

    /// <summary>
    ///     是否需重启生效（「重启后生效」标记由工单 03 消费）
    /// </summary>
    public bool RequiresRestart { get; } = contribution.RequiresRestart;

    /// <summary>
    ///     设置值类型（声明属性的类型）
    /// </summary>
    protected Type ValueType { get; } = contribution.ValueType;

    /// <summary>
    ///     读当前值：已修改为用户值，未修改为声明默认值
    /// </summary>
    protected object? GetValue()
    {
        return GetMethod.MakeGenericMethod(ValueType).Invoke(settings, [Id]);
    }

    /// <summary>
    ///     写新值：更新内存 + 防抖落盘 + 广播变更事件
    /// </summary>
    protected void SetValue(object? value)
    {
        SetMethod.MakeGenericMethod(ValueType).Invoke(settings, [Id, value]);
    }
}
