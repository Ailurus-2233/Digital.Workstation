using DigitalWorkstation.Core.Abstractions.Menus;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Resource;

namespace DigitalWorkstation.Core.Framework.Menus;

/// <summary>
///     菜单建树器：把扁平的菜单贡献列表构建为分组排序好的菜单树（纯函数，无状态）。
///     排序与分隔线规则见 ADR-0001：顶层只按 NodeOrder 排序、不分组不插分隔线；
///     子菜单按 (GroupOrder, Group) 分组、组内按 Order 排序、组间插分隔线；
///     同组同 Order 按解析后的标题字典序（Ordinal）定平局
/// </summary>
public static class MenuTreeBuilder
{
    /// <summary>
    ///     从全部菜单贡献构建顶层菜单列表；标题（路径段与条目）在此一次性解析
    /// </summary>
    public static IReadOnlyList<MenuTreeSubmenu> Build(IEnumerable<IMenuItemContribution> contributions)
    {
        var roots = new Dictionary<string, NodeAccum>(StringComparer.Ordinal);
        foreach (var contribution in contributions)
        {
            var segments = contribution.Path.Split('/', StringSplitOptions.TrimEntries);
            if (segments.Any(segment => segment.Length == 0))
            {
                Logger.Warning($"菜单路径 \"{contribution.Path}\" 含空段，跳过条目 \"{contribution.Title}\"",
                    nameof(MenuTreeBuilder));
                continue;
            }

            var node = roots.TryGetValue(segments[0], out var root)
                ? root
                : roots[segments[0]] = new NodeAccum(segments[0]);
            foreach (var segment in segments.Skip(1))
            {
                node = node.Children.TryGetValue(segment, out var child)
                    ? child
                    : node.Children[segment] = new NodeAccum(segment);
            }

            if (segments.Length == 1)
            {
                // 单段路径：Order 声明顶层菜单位次，Group/GroupOrder 描述条目分组
                node.MergeNodeOrder(contribution.NodeOrder);
                node.Items.Add(new LeafAccum(contribution.Group, contribution.GroupOrder, contribution.Order,
                    contribution.Title, contribution.IconPath, contribution.Command));
            }
            else
            {
                // 多段路径：Group/GroupOrder/NodeOrder 声明末端子菜单节点的位次，条目进末端菜单默认组
                node.MergePlacement(contribution.Group, contribution.GroupOrder, contribution.NodeOrder);
                node.Items.Add(new LeafAccum(null, 0, contribution.Order,
                    contribution.Title, contribution.IconPath, contribution.Command));
            }
        }

        return roots.Values
            .OrderBy(node => node.NodeOrder)
            .ThenBy(node => Language.Get(node.Segment), StringComparer.Ordinal)
            .Select(Emit)
            .ToArray();
    }

    private static MenuTreeSubmenu Emit(NodeAccum node)
    {
        var entries = new List<(string? Group, int GroupOrder, int Order, string Title, MenuTreeEntry Entry)>();
        foreach (var item in node.Items)
        {
            entries.Add((item.Group, item.GroupOrder, item.Order, item.Title,
                new MenuTreeItem(item.Title, item.IconPath, item.Command)));
        }
        foreach (var child in node.Children.Values)
        {
            entries.Add((child.Group, child.GroupOrder, child.NodeOrder, Language.Get(child.Segment), Emit(child)));
        }

        var sorted = entries
            .OrderBy(entry => entry.GroupOrder)
            .ThenBy(entry => entry.Group is null ? 0 : 1)
            .ThenBy(entry => entry.Group, StringComparer.Ordinal)
            .ThenBy(entry => entry.Order)
            .ThenBy(entry => entry.Title, StringComparer.Ordinal)
            .ToArray();

        var children = new List<MenuTreeEntry>(sorted.Length * 2);
        (string? Group, int GroupOrder)? previousGroup = null;
        foreach (var entry in sorted)
        {
            var currentGroup = (entry.Group, entry.GroupOrder);
            if (previousGroup is { } previous && previous != currentGroup)
            {
                children.Add(MenuTreeSeparator.Instance);
            }
            previousGroup = currentGroup;
            children.Add(entry.Entry);
        }

        return new MenuTreeSubmenu(Language.Get(node.Segment), children);
    }

    private sealed record LeafAccum(string? Group, int GroupOrder, int Order, string Title, string? IconPath,
        System.Windows.Input.ICommand Command);

    private sealed class NodeAccum(string segment)
    {
        private bool _placementDeclared;

        public string Segment { get; } = segment;

        /// <summary>
        ///     节点在父菜单内的组（仅深度 ≥ 2 的节点由类 attribute 声明；顶层菜单恒为 null）
        /// </summary>
        public string? Group { get; private set; }

        public int GroupOrder { get; private set; }

        public int NodeOrder { get; private set; }

        public Dictionary<string, NodeAccum> Children { get; } = new(StringComparer.Ordinal);

        public List<LeafAccum> Items { get; } = [];

        /// <summary>
        ///     顶层菜单位次：只取单段路径贡献的 NodeOrder，多处声明取最小
        /// </summary>
        public void MergeNodeOrder(int nodeOrder)
        {
            if (!_placementDeclared)
            {
                _placementDeclared = true;
                NodeOrder = nodeOrder;
                return;
            }
            NodeOrder = Math.Min(NodeOrder, nodeOrder);
        }

        /// <summary>
        ///     子菜单节点位次：GroupOrder/NodeOrder 取最小；Group 取 GroupOrder 最小声明的组名，
        ///     同 GroupOrder 取组名 Ordinal 小者（确定性规则）
        /// </summary>
        public void MergePlacement(string? group, int groupOrder, int nodeOrder)
        {
            if (!_placementDeclared)
            {
                _placementDeclared = true;
                Group = group;
                GroupOrder = groupOrder;
                NodeOrder = nodeOrder;
                return;
            }

            NodeOrder = Math.Min(NodeOrder, nodeOrder);
            if (groupOrder < GroupOrder ||
                (groupOrder == GroupOrder && string.CompareOrdinal(group, Group) < 0))
            {
                Group = group;
                GroupOrder = groupOrder;
            }
        }
    }
}
