using CommunityToolkit.Mvvm.ComponentModel;
using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.Core.Resource;

namespace DigitalWorkstation.Settings.ViewModels;

/// <summary>
///     枚举设置项的编辑器模型（enum → 下拉框）：
///     选项为枚举全部成员，显示名在贡献指定的资源所有者中按「设置项名称键 + 成员名」查找，
///     缺键回退完整组合键本身。保存时提交选中的成员。
/// </summary>
public sealed partial class EnumSettingItemModel : SettingItemModel
{
    private EnumOption? _savedOption;
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
                ResourceText.Get(contribution.ResourceType, $"{NameKey}{Enum.GetName(ValueType, value)}")))
            .ToArray();
        var current = GetValue();
        _savedOption = _selectedOption = Options.FirstOrDefault(option => Equals(option.Value, current));
    }

    /// <summary>
    ///     全部选项（按枚举声明顺序）
    /// </summary>
    public IReadOnlyList<EnumOption> Options { get; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasDraft))]
    private EnumOption? _selectedOption;

    public override bool HasDraft => SelectedOption is not null && SelectedOption != _savedOption;
    protected override void AcceptDraft() => _savedOption = SelectedOption;

    internal override object? DraftValue => SelectedOption?.Value;
}
