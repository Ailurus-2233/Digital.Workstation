namespace DigitalWorkstation.Core.Framework.Shell;

/// <summary>
///     Shell 布局状态 store：原型验证过的 reducer 的正式实现。
///     所有转换返回新实例，非法操作（如面板收起时激活 tab）被拒绝并返回等值状态。
/// </summary>
public sealed record ShellLayoutState
{
    /// <summary>
    ///     当前选中的 ActivityBar 导航项 Id；SideBar 收起时保留
    /// </summary>
    public string? SelectedActivity { get; init; }

    public SideBarState SideBar { get; init; } = new();

    public AuxiliaryPanelState AuxiliaryPanel { get; init; } = new();

    public BottomPanelState BottomPanel { get; init; } = new();

    public MainContentState MainContent { get; init; } = new();

    public static ShellLayoutState Initial => new();

    /// <summary>
    ///     选中导航项：已选中且 SideBar 可见 → 收起（选中项保留）；
    ///     否则选中该导航项、展开 SideBar 并切换其内容。不改变 MainContent。
    /// </summary>
    public ShellLayoutState SelectActivity(string id)
    {
        if (SelectedActivity == id && SideBar.Visible)
        {
            return this with { SideBar = SideBar with { Visible = false } };
        }

        return this with
        {
            SelectedActivity = id,
            SideBar = SideBar with { Visible = true, ContentFor = id }
        };
    }

    /// <summary>
    ///     独立翻转 SideBar 可见性，不影响选中项与内容
    /// </summary>
    public ShellLayoutState ToggleSideBar()
    {
        return this with { SideBar = SideBar with { Visible = !SideBar.Visible } };
    }

    /// <summary>
    ///     独立翻转 AuxiliaryPanel 可见性，不影响活动 tab
    /// </summary>
    public ShellLayoutState ToggleAuxiliaryPanel()
    {
        return this with { AuxiliaryPanel = AuxiliaryPanel with { Visible = !AuxiliaryPanel.Visible } };
    }

    /// <summary>
    ///     独立翻转 BottomPanel 可见性，不影响活动 tab
    /// </summary>
    public ShellLayoutState ToggleBottomPanel()
    {
        return this with { BottomPanel = BottomPanel with { Visible = !BottomPanel.Visible } };
    }

    /// <summary>
    ///     切换 AuxiliaryPanel 活动 tab；面板收起时拒绝（状态不变）
    /// </summary>
    public ShellLayoutState ActivateAuxTab(string tab)
    {
        if (!AuxiliaryPanel.Visible)
        {
            return this;
        }

        return this with { AuxiliaryPanel = AuxiliaryPanel with { ActiveTab = tab } };
    }

    /// <summary>
    ///     切换 BottomPanel 活动 tab；面板收起时拒绝（状态不变）
    /// </summary>
    public ShellLayoutState ActivateBottomTab(string tab)
    {
        if (!BottomPanel.Visible)
        {
            return this;
        }

        return this with { BottomPanel = BottomPanel with { ActiveTab = tab } };
    }

    /// <summary>
    ///     切换 MainContent 活动视图，仅改 MainContent
    /// </summary>
    public ShellLayoutState OpenMainView(string view)
    {
        return this with { MainContent = MainContent with { ActiveView = view } };
    }

    /// <summary>
    ///     调整区域尺寸并 clamp 到合理区间；BottomPanel 调整高度，其余调整宽度
    /// </summary>
    public ShellLayoutState Resize(PanelResizeTarget target, double delta)
    {
        return target switch
        {
            PanelResizeTarget.SideBar => this with
            {
                SideBar = SideBar with
                {
                    Width = Clamp(SideBar.Width + delta, SideBarState.MinWidth, SideBarState.MaxWidth)
                }
            },
            PanelResizeTarget.AuxiliaryPanel => this with
            {
                AuxiliaryPanel = AuxiliaryPanel with
                {
                    Width = Clamp(AuxiliaryPanel.Width + delta, AuxiliaryPanelState.MinWidth,
                        AuxiliaryPanelState.MaxWidth)
                }
            },
            PanelResizeTarget.BottomPanel => this with
            {
                BottomPanel = BottomPanel with
                {
                    Height = Clamp(BottomPanel.Height + delta, BottomPanelState.MinHeight,
                        BottomPanelState.MaxHeight)
                }
            },
            _ => this
        };
    }

    private static double Clamp(double value, double min, double max)
    {
        return Math.Max(min, Math.Min(max, value));
    }
}
