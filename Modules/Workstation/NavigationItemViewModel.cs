using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using DigitalWorkstation.Core.Abstractions.Shell;

namespace DigitalWorkstation.Workstation;

/// <summary>
///     ActivityBar 导航项的呈现模型：包装贡献元数据并解析图标几何
/// </summary>
public partial class NavigationItemViewModel : ObservableObject
{
    public NavigationItemViewModel(INavigationItemContribution contribution)
    {
        Contribution = contribution;
        Icon = StreamGeometry.Parse(contribution.IconPath);
    }

    public INavigationItemContribution Contribution { get; }

    public string Id => Contribution.Id;

    public string Title => Contribution.Title;

    /// <summary>
    ///     由 <see cref="INavigationItemContribution.IconPath" /> 解析的图标几何，随主题变色
    /// </summary>
    public Geometry Icon { get; }

    [ObservableProperty]
    private bool _isSelected;
}
