using System.Windows.Input;

namespace DigitalWorkstation.Core.Framework.Menus;

/// <summary>
///     菜单树的条目：叶子菜单项（<see cref="MenuTreeItem" />）、子菜单节点（<see cref="MenuTreeSubmenu" />）
///     或分隔线（<see cref="MenuTreeSeparator" />）。由 <see cref="MenuTreeBuilder" /> 从贡献建树生成，
///     shell 侧再转换为 Avalonia 控件
/// </summary>
public abstract record MenuTreeEntry;

/// <summary>
///     叶子菜单项：标题已按当前 UI 区域性解析
/// </summary>
public sealed record MenuTreeItem(string Title, string? IconPath, ICommand Command) : MenuTreeEntry;

/// <summary>
///     子菜单节点（含顶层菜单）：标题已解析；Children 中的分隔线已按分组规则插好，
///     不存在开头/结尾/连续分隔线
/// </summary>
public sealed record MenuTreeSubmenu(string Title, IReadOnlyList<MenuTreeEntry> Children) : MenuTreeEntry;

/// <summary>
///     组间分隔线标记，单例；shell 侧转换为 Avalonia Separator 控件
/// </summary>
public sealed record MenuTreeSeparator : MenuTreeEntry
{
    public static readonly MenuTreeSeparator Instance = new();

    private MenuTreeSeparator()
    {
    }
}
