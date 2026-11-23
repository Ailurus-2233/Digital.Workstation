using CommunityToolkit.Mvvm.ComponentModel;
using DigitalWorkstation.Core.Abstractions.Settings;

namespace DigitalWorkstation.Settings.ViewModels;

/// <summary>Ordinary text settings only; secrets must use a separate credential store.</summary>
public sealed partial class StringSettingItemModel : SettingItemModel
{
    private string _savedValue;
    public StringSettingItemModel(SettingItemContribution contribution, ISettingsService settings) : base(contribution, settings)
    {
        _savedValue = _value = (string?)GetValue() ?? "";
    }
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasDraft))] private string _value;
    public override bool HasDraft => Value != _savedValue;
    protected override void AcceptDraft() => _savedValue = Value;
    internal override string DraftValue => Value;
}
