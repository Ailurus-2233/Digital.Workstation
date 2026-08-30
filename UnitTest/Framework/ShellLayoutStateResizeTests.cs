using DigitalWorkstation.Core.Framework.Shell;
using Xunit;

namespace DigitalWorkstation.UnitTest.Framework;

/// <summary>
///     ShellLayoutState 的 resize 转换：给定 state 与 resize action，断言新 state。
///     clamp 边界取自各区域状态记录的 Min/Max 常量
/// </summary>
public class ShellLayoutStateResizeTests
{
    [Fact]
    public void ResizeSideBar_IncreasesWidthByDelta()
    {
        var state = ShellLayoutState.Initial;

        var next = state.Resize(PanelResizeTarget.SideBar, 40);

        Assert.Equal(state.SideBar.Width + 40, next.SideBar.Width);
    }

    [Fact]
    public void ResizeSideBar_DecreasesWidthByDelta()
    {
        var state = ShellLayoutState.Initial;

        var next = state.Resize(PanelResizeTarget.SideBar, -40);

        Assert.Equal(state.SideBar.Width - 40, next.SideBar.Width);
    }

    [Fact]
    public void ResizeSideBar_ClampsAtMaxWidth()
    {
        var state = ShellLayoutState.Initial;

        var next = state.Resize(PanelResizeTarget.SideBar, 10000);

        Assert.Equal(SideBarState.MaxWidth, next.SideBar.Width);
    }

    [Fact]
    public void ResizeSideBar_ClampsAtMinWidth()
    {
        var state = ShellLayoutState.Initial;

        var next = state.Resize(PanelResizeTarget.SideBar, -10000);

        Assert.Equal(SideBarState.MinWidth, next.SideBar.Width);
    }

    [Fact]
    public void ResizeAuxiliaryPanel_AdjustsWidthAndClampsToLimits()
    {
        var state = ShellLayoutState.Initial;

        var wider = state.Resize(PanelResizeTarget.AuxiliaryPanel, 40);
        Assert.Equal(state.AuxiliaryPanel.Width + 40, wider.AuxiliaryPanel.Width);

        var clampedMax = state.Resize(PanelResizeTarget.AuxiliaryPanel, 10000);
        Assert.Equal(AuxiliaryPanelState.MaxWidth, clampedMax.AuxiliaryPanel.Width);

        var clampedMin = state.Resize(PanelResizeTarget.AuxiliaryPanel, -10000);
        Assert.Equal(AuxiliaryPanelState.MinWidth, clampedMin.AuxiliaryPanel.Width);
    }

    [Fact]
    public void ResizeBottomPanel_AdjustsHeightAndClampsToLimits()
    {
        var state = ShellLayoutState.Initial;

        var taller = state.Resize(PanelResizeTarget.BottomPanel, 40);
        Assert.Equal(state.BottomPanel.Height + 40, taller.BottomPanel.Height);

        var clampedMax = state.Resize(PanelResizeTarget.BottomPanel, 10000);
        Assert.Equal(BottomPanelState.MaxHeight, clampedMax.BottomPanel.Height);

        var clampedMin = state.Resize(PanelResizeTarget.BottomPanel, -10000);
        Assert.Equal(BottomPanelState.MinHeight, clampedMin.BottomPanel.Height);
    }

    [Fact]
    public void ResizeSideBar_LeavesOtherRegionsUntouched()
    {
        var state = ShellLayoutState.Initial;

        var next = state.Resize(PanelResizeTarget.SideBar, 40);

        Assert.Equal(state.AuxiliaryPanel, next.AuxiliaryPanel);
        Assert.Equal(state.BottomPanel, next.BottomPanel);
        Assert.Equal(state.MainContent, next.MainContent);
        Assert.Equal(state.SelectedActivity, next.SelectedActivity);
    }

    [Fact]
    public void ResizeBottomPanel_LeavesWidthsUntouched()
    {
        var state = ShellLayoutState.Initial;

        var next = state.Resize(PanelResizeTarget.BottomPanel, 40);

        Assert.Equal(state.SideBar.Width, next.SideBar.Width);
        Assert.Equal(state.AuxiliaryPanel.Width, next.AuxiliaryPanel.Width);
    }

    [Fact]
    public void CollapsedSideBar_KeepsResizedWidth_WhenRestored()
    {
        var resized = ShellLayoutState.Initial.Resize(PanelResizeTarget.SideBar, 60);

        var collapsed = resized.ToggleSideBar();
        Assert.Equal(resized.SideBar.Width, collapsed.SideBar.Width);

        var restored = collapsed.ToggleSideBar();
        Assert.Equal(resized.SideBar.Width, restored.SideBar.Width);
    }

    [Fact]
    public void CollapsedBottomPanel_KeepsResizedHeight_WhenRestored()
    {
        var resized = ShellLayoutState.Initial.Resize(PanelResizeTarget.BottomPanel, 60);

        var collapsed = resized.ToggleBottomPanel();
        Assert.Equal(resized.BottomPanel.Height, collapsed.BottomPanel.Height);

        var restored = collapsed.ToggleBottomPanel();
        Assert.Equal(resized.BottomPanel.Height, restored.BottomPanel.Height);
    }

    [Fact]
    public void CollapsedAuxiliaryPanel_KeepsResizedWidth_WhenRestored()
    {
        var resized = ShellLayoutState.Initial.Resize(PanelResizeTarget.AuxiliaryPanel, 60);

        var collapsed = resized.ToggleAuxiliaryPanel();
        Assert.Equal(resized.AuxiliaryPanel.Width, collapsed.AuxiliaryPanel.Width);

        var restored = collapsed.ToggleAuxiliaryPanel();
        Assert.Equal(resized.AuxiliaryPanel.Width, restored.AuxiliaryPanel.Width);
    }
}
