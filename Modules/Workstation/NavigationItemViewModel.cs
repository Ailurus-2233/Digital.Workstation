using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using DigitalWorkstation.Core.Abstractions.Contributions;

namespace DigitalWorkstation.Workstation;

/// <summary>
///     ActivityBar 导航项的呈现模型：包装工具视图元数据并解析图标几何
/// </summary>
public partial class NavigationItemViewModel : ObservableObject
{
    public NavigationItemViewModel(ToolViewContribution contribution)
    {
        Contribution = contribution;
        Icon = contribution.IconPath is { } path ? StreamGeometry.Parse(path) : null;
    }

    public ToolViewContribution Contribution { get; }

    public string Id => Contribution.Id;

    public string Title => Contribution.Title;

    /// <summary>
    ///     由 <see cref="ToolViewContribution.IconPath" /> 解析的图标几何，随主题变色；null = 无图标
    /// </summary>
    public Geometry? Icon { get; }

    [ObservableProperty]
    private bool _isSelected;
}
