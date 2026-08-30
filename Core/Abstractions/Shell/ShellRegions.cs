namespace DigitalWorkstation.Core.Abstractions.Shell;

/// <summary>
///     Shell 布局的 Prism Region 名称常量
/// </summary>
public static class ShellRegions
{
    /// <summary>
    ///     工作区最左侧的竖向导航栏
    /// </summary>
    public const string ActivityBar = nameof(ActivityBar);

    /// <summary>
    ///     ActivityBar 右侧的容器，显示当前选中导航项的内容
    /// </summary>
    public const string SideBar = nameof(SideBar);

    /// <summary>
    ///     工作区中央的主 Region，单视图切换
    /// </summary>
    public const string MainContent = nameof(MainContent);

    /// <summary>
    ///     工作区右侧的 tab + 容器区域
    /// </summary>
    public const string AuxiliaryPanel = nameof(AuxiliaryPanel);

    /// <summary>
    ///     工作区底部的 tab + 容器区域
    /// </summary>
    public const string BottomPanel = nameof(BottomPanel);
}
