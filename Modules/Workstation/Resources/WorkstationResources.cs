using DigitalWorkstation.Core.Resource;

namespace DigitalWorkstation.Workstation.Resources;

/// <summary>
///     所属模块的界面文案；中性资源为中文，en-US 为英文。
/// </summary>
public static class WorkstationResources
{
    /// <summary>主页产品说明。</summary>
    public static string HomeDescription => ResourceText.Get(typeof(WorkstationResources), nameof(HomeDescription));

    /// <summary>主页已加载模块区域标题。</summary>
    public static string HomeLoadedModulesTitle => ResourceText.Get(typeof(WorkstationResources), nameof(HomeLoadedModulesTitle));

    /// <summary>主页已加载插件区域标题。</summary>
    public static string HomeLoadedPluginsTitle => ResourceText.Get(typeof(WorkstationResources), nameof(HomeLoadedPluginsTitle));

    /// <summary>主页系统核心分组标题。</summary>
    public static string HomeCoreTitle => ResourceText.Get(typeof(WorkstationResources), nameof(HomeCoreTitle));

    /// <summary>主页自定义模块分组标题。</summary>
    public static string HomeCustomTitle => ResourceText.Get(typeof(WorkstationResources), nameof(HomeCustomTitle));

    /// <summary>主页未加载模块时的提示。</summary>
    public static string HomeNoModules => ResourceText.Get(typeof(WorkstationResources), nameof(HomeNoModules));

    /// <summary>主页未加载插件时的提示。</summary>
    public static string HomeNoPlugins => ResourceText.Get(typeof(WorkstationResources), nameof(HomeNoPlugins));

    /// <summary>
    ///     关于窗口标题
    /// </summary>
    public static string AboutWindowTitle => ResourceText.Get(typeof(WorkstationResources), nameof(AboutWindowTitle));

    /// <summary>
    ///     shell 预置"设置"导航项的标题
    /// </summary>
    public static string SettingsNavigationTitle => ResourceText.Get(typeof(WorkstationResources), nameof(SettingsNavigationTitle));

    /// <summary>
    ///     顶层"文件"菜单的标题
    /// </summary>
    public static string MenuFileTitle => ResourceText.Get(typeof(WorkstationResources), nameof(MenuFileTitle));

    /// <summary>
    ///     顶层"视图"菜单的标题
    /// </summary>
    public static string MenuViewTitle => ResourceText.Get(typeof(WorkstationResources), nameof(MenuViewTitle));

    /// <summary>
    ///     顶层"帮助"菜单的标题
    /// </summary>
    public static string MenuHelpTitle => ResourceText.Get(typeof(WorkstationResources), nameof(MenuHelpTitle));

    /// <summary>
    ///     文件菜单"退出"项的标题
    /// </summary>
    public static string MenuExitTitle => ResourceText.Get(typeof(WorkstationResources), nameof(MenuExitTitle));

    /// <summary>
    ///     回到主页命令与文件菜单项的标题
    /// </summary>
    public static string ReturnHomeTitle => ResourceText.Get(typeof(WorkstationResources), nameof(ReturnHomeTitle));

    /// <summary>
    ///     文件菜单"首选项"项的标题
    /// </summary>
    public static string MenuPreferencesTitle => ResourceText.Get(typeof(WorkstationResources), nameof(MenuPreferencesTitle));

    /// <summary>
    ///     帮助菜单"关于"项的标题
    /// </summary>
    public static string MenuAboutTitle => ResourceText.Get(typeof(WorkstationResources), nameof(MenuAboutTitle));

    /// <summary>
    ///     SideBar 显隐切换项的标题（视图菜单）
    /// </summary>
    public static string ToggleSideBarTitle => ResourceText.Get(typeof(WorkstationResources), nameof(ToggleSideBarTitle));

    /// <summary>
    ///     BottomPanel 显隐切换项的标题（视图菜单）
    /// </summary>
    public static string ToggleBottomPanelTitle => ResourceText.Get(typeof(WorkstationResources), nameof(ToggleBottomPanelTitle));

    /// <summary>
    ///     AuxiliaryPanel 显隐切换项的标题（视图菜单）
    /// </summary>
    public static string ToggleAuxiliaryPanelTitle => ResourceText.Get(typeof(WorkstationResources), nameof(ToggleAuxiliaryPanelTitle));

    /// <summary>
    ///     面板对齐菜单"左对齐"项的标题
    /// </summary>
    public static string PanelAlignLeftTitle => ResourceText.Get(typeof(WorkstationResources), nameof(PanelAlignLeftTitle));

    /// <summary>
    ///     面板对齐菜单"右对齐"项的标题
    /// </summary>
    public static string PanelAlignRightTitle => ResourceText.Get(typeof(WorkstationResources), nameof(PanelAlignRightTitle));

    /// <summary>
    ///     面板对齐菜单"居中"项的标题
    /// </summary>
    public static string PanelAlignCenterTitle => ResourceText.Get(typeof(WorkstationResources), nameof(PanelAlignCenterTitle));

    /// <summary>
    ///     面板对齐菜单"两端对齐"项的标题
    /// </summary>
    public static string PanelAlignJustifyTitle => ResourceText.Get(typeof(WorkstationResources), nameof(PanelAlignJustifyTitle));

    /// <summary>
    ///     视图菜单"重置布局"项的标题
    /// </summary>
    public static string ResetLayoutTitle => ResourceText.Get(typeof(WorkstationResources), nameof(ResetLayoutTitle));

    /// <summary>
    ///     shell 预置状态栏"就绪"项的文本
    /// </summary>
    public static string StatusReadyTitle => ResourceText.Get(typeof(WorkstationResources), nameof(StatusReadyTitle));
}
