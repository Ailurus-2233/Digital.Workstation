using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace DigitalWorkstation.Core.Framework.Menus;

public sealed record TabMenuAction(string Title, ICommand Command, string Group, int GroupOrder, int Order,
    object? CommandParameter = null);

/// <summary>在主视图根控件上设置或绑定 Items。每次打开标签菜单时读取当前页面的配置。</summary>
public sealed class TabMenu : AvaloniaObject
{
    public static readonly AttachedProperty<IEnumerable<TabMenuAction>?> ItemsProperty =
        AvaloniaProperty.RegisterAttached<TabMenu, Control, IEnumerable<TabMenuAction>?>("Items");

    public static IEnumerable<TabMenuAction>? GetItems(Control page) => page.GetValue(ItemsProperty);
    public static void SetItems(Control page, IEnumerable<TabMenuAction>? items) => page.SetValue(ItemsProperty, items);

    public static IReadOnlyList<Control> BuildItems(IEnumerable<MenuItem> common, Control? page)
    {
        var result = new List<Control>(common);
        var actions = page is null ? null : GetItems(page);
        foreach (var group in MenuTreeBuilder.OrderedGroups(actions ?? [],
                     action => (action.GroupOrder, action.Group), action => action.Order, action => action.Title))
        {
            if (result.Count > 0) result.Add(new Separator());
            result.AddRange(group.Select(action => new MenuItem
            {
                Header = action.Title, Command = action.Command, CommandParameter = action.CommandParameter
            }));
        }
        return result;
    }
}
