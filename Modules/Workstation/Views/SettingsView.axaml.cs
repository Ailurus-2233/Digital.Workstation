using Avalonia.Controls;
using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.UIPackage;

namespace DigitalWorkstation.Workstation.Views;

/// <summary>
///     shell 预置的"设置"工具视图，钉在 ActivityBar 底部段（不可拖拽，ADR-0002）
/// </summary>
[ToolView("shell.settings", "SettingsNavigationTitle", Icon = Icons.Settings,
    Default = ToolViewPlacement.ActivityBar, AllowMove = false)]
public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }
}
