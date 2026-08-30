using Avalonia.Media;
using DigitalWorkstation.Core.Abstractions.Shell;

namespace DigitalWorkstation.Workstation;

/// <summary>
///     状态栏条目的呈现模型：包装贡献元数据并解析图标几何
/// </summary>
public class StatusBarItemViewModel
{
    public StatusBarItemViewModel(IStatusBarItemContribution contribution)
    {
        Contribution = contribution;
        Icon = StreamGeometry.Parse(contribution.IconPath);
    }

    public IStatusBarItemContribution Contribution { get; }

    public string Id => Contribution.Id;

    public string Title => Contribution.Title;

    /// <summary>
    ///     由 <see cref="IStatusBarItemContribution.IconPath" /> 解析的图标几何，随主题变色
    /// </summary>
    public Geometry Icon { get; }
}
