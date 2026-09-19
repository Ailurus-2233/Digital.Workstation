using DigitalWorkstation.Core.Abstractions.Contributions;

namespace DigitalWorkstation.Core.Framework.Layout;

/// <summary>
///     布局配置与运行态的边界：归属恢复、孤儿过滤、默认值及尺寸校验集中于此，不读取 UI 集合。
/// </summary>
public static class ShellLayoutConfiguration
{
    public static (ShellLayoutState State, PanelAlignment Alignment) Restore(
        IReadOnlyList<ToolViewContribution> contributions, ShellLayoutDto? layout, MainContentState mainContent)
    {
        var placements = layout?.Placements ?? [];
        bool HasPlacement(string id) => placements.TryGetValue(id, out var entry) &&
                                       entry is not null && Enum.IsDefined(entry.Bar);
        string[] Items(ToolViewPlacement bar) => contributions
            .Where(view => view.AllowMove && HasPlacement(view.Id) && placements[view.Id].Bar == bar)
            .OrderBy(view => placements[view.Id].Index)
            .Concat(contributions.Where(view => view.AllowMove && !HasPlacement(view.Id) && view.Placement == bar)
                .OrderBy(view => view.Order))
            .Select(view => view.Id).ToArray();
        static string? Active(IReadOnlyList<string> items, string? preferred) =>
            preferred is not null && items.Contains(preferred) ? preferred : items.FirstOrDefault();

        var activity = Items(ToolViewPlacement.ActivityBar);
        var auxiliary = Items(ToolViewPlacement.AuxiliaryPanel);
        var bottom = Items(ToolViewPlacement.BottomPanel);
        var selected = layout?.SideBar?.Selected;
        if (selected is not null && !activity.Contains(selected) &&
            !contributions.Any(view => view.Id == selected && !view.AllowMove &&
                                       view.Placement == ToolViewPlacement.ActivityBar))
            selected = null;

        var initial = ShellLayoutState.Initial;
        var state = initial with
        {
            MainContent = mainContent,
            ActivityBarItems = activity,
            SelectedActivity = selected,
            SideBar = initial.SideBar with
            {
                ContentFor = selected,
                Visible = layout?.SideBar?.Visible ?? initial.SideBar.Visible,
                Width = Math.Clamp(layout?.SideBar?.Width ?? initial.SideBar.Width,
                    SideBarState.MinWidth, SideBarState.MaxWidth)
            },
            AuxiliaryPanel = initial.AuxiliaryPanel with
            {
                Tabs = auxiliary,
                ActiveTab = Active(auxiliary, layout?.AuxiliaryPanel?.ActiveTab),
                Visible = layout?.AuxiliaryPanel?.Visible ?? initial.AuxiliaryPanel.Visible,
                Width = Math.Clamp(layout?.AuxiliaryPanel?.Width ?? initial.AuxiliaryPanel.Width,
                    AuxiliaryPanelState.MinWidth, AuxiliaryPanelState.MaxWidth)
            },
            BottomPanel = initial.BottomPanel with
            {
                Tabs = bottom,
                ActiveTab = Active(bottom, layout?.BottomPanel?.ActiveTab),
                Visible = layout?.BottomPanel?.Visible ?? initial.BottomPanel.Visible,
                Height = Math.Clamp(layout?.BottomPanel?.Height ?? initial.BottomPanel.Height,
                    BottomPanelState.MinHeight, BottomPanelState.MaxHeight)
            }
        };
        var alignment = layout?.PanelAlignment ?? PanelAlignment.Center;
        return (state, Enum.IsDefined(alignment) ? alignment : PanelAlignment.Center);
    }

    public static ShellLayoutDto Capture(ShellLayoutState state, PanelAlignment alignment)
    {
        var placements = new Dictionary<string, ToolViewPlacementEntry>(StringComparer.Ordinal);
        void Add(IReadOnlyList<string> ids, ToolViewPlacement bar)
        {
            for (var i = 0; i < ids.Count; i++)
                placements[ids[i]] = new ToolViewPlacementEntry { Bar = bar, Index = i };
        }
        Add(state.ActivityBarItems, ToolViewPlacement.ActivityBar);
        Add(state.AuxiliaryPanel.Tabs, ToolViewPlacement.AuxiliaryPanel);
        Add(state.BottomPanel.Tabs, ToolViewPlacement.BottomPanel);
        return new ShellLayoutDto
        {
            PanelAlignment = alignment,
            Placements = placements,
            SideBar = new SideBarLayoutDto
            {
                Visible = state.SideBar.Visible, Width = state.SideBar.Width, Selected = state.SelectedActivity
            },
            AuxiliaryPanel = new PanelLayoutDto
            {
                Visible = state.AuxiliaryPanel.Visible, Width = state.AuxiliaryPanel.Width,
                ActiveTab = state.AuxiliaryPanel.ActiveTab
            },
            BottomPanel = new BottomPanelLayoutDto
            {
                Visible = state.BottomPanel.Visible, Height = state.BottomPanel.Height,
                ActiveTab = state.BottomPanel.ActiveTab
            }
        };
    }
}
