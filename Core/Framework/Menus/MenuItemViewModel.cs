using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;

namespace DigitalWorkstation.Core.Framework.Menus;

/// <summary>
///     菜单项的呈现模型：叶子（<see cref="Command" /> 非空）或子菜单节点（<see cref="Children" /> 非空），
///     由菜单树（<see cref="MenuTreeSubmenu" />）递归转换而来；分隔线直接是 Avalonia Separator 控件。
///     shell 宿主窗口的 ViewModel 经 MenuBarItems 暴露顶层集合（宽松绑定约定）
/// </summary>
public class MenuItemViewModel
{
    private MenuItemViewModel()
    {
    }

    public required string Title { get; init; }

    /// <summary>
    ///     图标几何（随主题变色）；null = 无图标，模板不渲染 PathIcon
    /// </summary>
    public Geometry? Icon { get; init; }

    public ICommand? Command { get; init; }

    public ObservableCollection<object> Children { get; } = [];

    /// <summary>
    ///     把建树器产出的子菜单节点递归转换为呈现模型
    /// </summary>
    public static MenuItemViewModel FromSubmenu(MenuTreeSubmenu submenu)
    {
        var viewModel = new MenuItemViewModel { Title = submenu.Title };
        foreach (var entry in submenu.Children)
        {
            viewModel.Children.Add(entry switch
            {
                MenuTreeItem item => new MenuItemViewModel
                {
                    Title = item.Title,
                    Icon = item.IconPath is null ? null : StreamGeometry.Parse(item.IconPath),
                    Command = item.Command
                },
                MenuTreeSubmenu child => FromSubmenu(child),
                _ => new Separator()
            });
        }
        return viewModel;
    }
}
