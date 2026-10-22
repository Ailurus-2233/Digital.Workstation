using CommunityToolkit.Mvvm.ComponentModel;
using DigitalWorkstation.Core.Abstractions.Settings;

namespace DigitalWorkstation.Settings.ViewModels;

/// <summary>Ordinary text settings only; secrets must use a separate credential store.</summary>
public sealed partial class StringSettingItemModel : SettingItemModel
{
    public StringSettingItemModel(SettingItemContribution contribution, ISettingsService settings) : base(contribution, settings)
    {
        _value = (string?)GetValue() ?? "";
    }
    [ObservableProperty] private string _value;
    partial void OnValueChanged(string value)
    {
        if (!Equals(value, GetValue())) SetValue(value);
    }
}
