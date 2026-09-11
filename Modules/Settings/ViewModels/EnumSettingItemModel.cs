using CommunityToolkit.Mvvm.ComponentModel;
using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.Core.Resource;

namespace DigitalWorkstation.Settings.ViewModels;

/// <summary>
///     枚举设置项的编辑器模型（ADR-0006 决策 8：enum → 下拉框）：
///     选项为枚举全部成员，显示名走 Language 资源键（键按「设置项名称键 + 成员名」约定生成，
///     缺键回退成员名本身）；改选即写入 ISettingsService（落盘 + 广播变更事件）
/// </summary>
public sealed partial class EnumSettingItemModel : SettingItemModel
{
    /// <summary>
    ///     下拉框选项：枚举成员值 + 本地化显示名
    /// </summary>
    public sealed record EnumOption(object Value, string DisplayName);

    public EnumSettingItemModel(SettingItemContribution contribution, ISettingsService settings)
        : base(contribution, settings)
    {
        Options = Enum.GetValues(ValueType)
            .Cast<object>()
            .Select(value => new EnumOption(value,
                Language.Get($"{NameKey}{Enum.GetName(ValueType, value)}")))
            .ToArray();
        // 直接赋字段而不走属性，避免把「读取当前值」误触发成一次写入
        _selectedOption = Options.FirstOrDefault(option => Equals(option.Value, GetValue()));
    }

    /// <summary>
    ///     全部选项（按枚举声明顺序）
    /// </summary>
    public IReadOnlyList<EnumOption> Options { get; }

    [ObservableProperty]
    private EnumOption? _selectedOption;

    partial void OnSelectedOptionChanged(EnumOption? value)
    {
        // 与当前值相同不写入：不把「未修改」误落成一条用户修改记录
        if (value is null || Equals(value.Value, GetValue()))
        {
            return;
        }

        SetValue(value.Value);
    }
}
