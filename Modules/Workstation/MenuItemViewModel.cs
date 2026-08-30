using System.Windows.Input;
using Avalonia.Media;
using DigitalWorkstation.Core.Abstractions.Shell;

namespace DigitalWorkstation.Workstation;

/// <summary>
///     菜单栏菜单项的呈现模型：包装贡献元数据并解析图标几何
/// </summary>
public class MenuItemViewModel
{
    public MenuItemViewModel(IMenuItemContribution contribution)
    {
        Contribution = contribution;
        Icon = StreamGeometry.Parse(contribution.IconPath);
    }

    public IMenuItemContribution Contribution { get; }

    public string Id => Contribution.Id;

    public string Title => Contribution.Title;

    /// <summary>
    ///     由 <see cref="IMenuItemContribution.IconPath" /> 解析的图标几何，随主题变色
    /// </summary>
    public Geometry Icon { get; }

    public ICommand Command => Contribution.Command;
}
