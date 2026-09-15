using Avalonia.Controls;

namespace DigitalWorkstation.Settings.Views;

/// <summary>
///     设置页：左侧设置分组树、右侧选中分组的设置项编辑器（ADR-0006 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 5）
/// </summary>
public partial class SettingsPageView : UserControl
{
    public SettingsPageView()
    {
        InitializeComponent();
    }
}
