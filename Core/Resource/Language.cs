using System.Resources;

namespace DigitalWorkstation.Core.Resource;

/// <summary>
///     界面文案的统一入口：按当前 UI 区域性读取语言资源（中性资源为中文，en-US 为英文卫星程序集）。
///     C# 中的显示字符串一律经本类获取，不直接硬编码
/// </summary>
public static class Language
{
    private static readonly ResourceManager Manager =
        new("DigitalWorkstation.Core.Resource.Language", typeof(Language).Assembly);

    /// <summary>
    ///     按键取文案；键缺失时返回键本身，便于发现遗漏
    /// </summary>
    public static string Get(string key)
    {
        return Manager.GetString(key) ?? key;
    }

    /// <summary>
    ///     shell 预置"设置"导航项的标题
    /// </summary>
    public static string SettingsNavigationTitle => Get(nameof(SettingsNavigationTitle));

    /// <summary>
    ///     DashBoard 导航项的标题
    /// </summary>
    public static string DashBoardNavigationTitle => Get(nameof(DashBoardNavigationTitle));
    /// <summary>
    ///     shell 预置 AuxiliaryPanel 演示 tab"属性"的标题
    /// </summary>
    public static string PropertiesTabTitle => Get(nameof(PropertiesTabTitle));

    /// <summary>
    ///     shell 预置 AuxiliaryPanel 演示 tab"大纲"的标题
    /// </summary>
    public static string OutlineTabTitle => Get(nameof(OutlineTabTitle));

    /// <summary>
    ///     shell 预置 BottomPanel 演示 tab"输出"的标题
    /// </summary>
    public static string OutputTabTitle => Get(nameof(OutputTabTitle));

    /// <summary>
    ///     shell 预置 BottomPanel 演示 tab"日志"的标题
    /// </summary>
    public static string LogTabTitle => Get(nameof(LogTabTitle));

    /// <summary>
    ///     DashBoard 贡献给 BottomPanel 的演示 tab"任务"的标题
    /// </summary>
    public static string DashBoardTasksTabTitle => Get(nameof(DashBoardTasksTabTitle));
    /// <summary>
    ///     文件菜单"退出"项的标题
    /// </summary>
    public static string MenuExitTitle => Get(nameof(MenuExitTitle));

    /// <summary>
    ///     帮助菜单"关于"项的标题
    /// </summary>
    public static string MenuAboutTitle => Get(nameof(MenuAboutTitle));

    /// <summary>
    ///     SideBar 显隐切换项的标题（视图菜单与快速工具栏共用）
    /// </summary>
    public static string ToggleSideBarTitle => Get(nameof(ToggleSideBarTitle));

    /// <summary>
    ///     BottomPanel 显隐切换项的标题（视图菜单与快速工具栏共用）
    /// </summary>
    public static string ToggleBottomPanelTitle => Get(nameof(ToggleBottomPanelTitle));

    /// <summary>
    ///     AuxiliaryPanel 显隐切换项的标题（视图菜单与快速工具栏共用）
    /// </summary>
    public static string ToggleAuxiliaryPanelTitle => Get(nameof(ToggleAuxiliaryPanelTitle));

    /// <summary>
    ///     shell 预置状态栏"就绪"项的文本
    /// </summary>
    public static string StatusReadyTitle => Get(nameof(StatusReadyTitle));

    /// <summary>
    ///     DashBoard 贡献给文件菜单的"打开启动台"项的标题
    /// </summary>
    public static string DashBoardOpenWindowMenuTitle => Get(nameof(DashBoardOpenWindowMenuTitle));

    /// <summary>
    ///     DashBoard 贡献给快速工具栏的"概览"按钮的标题（ToolTip）
    /// </summary>
    public static string DashBoardOverviewToolBarTitle => Get(nameof(DashBoardOverviewToolBarTitle));

    /// <summary>
    ///     启动台显示进度前的初始阶段文本
    /// </summary>
    public static string SplashStartingText => Get(nameof(SplashStartingText));

    /// <summary>
    ///     启动台"初始化核心服务"阶段名
    /// </summary>
    public static string SplashPhaseCoreServices => Get(nameof(SplashPhaseCoreServices));

    /// <summary>
    ///     启动台"加载模块"阶段名
    /// </summary>
    public static string SplashPhaseLoadingModules => Get(nameof(SplashPhaseLoadingModules));

    /// <summary>
    ///     启动台"就绪"阶段名
    /// </summary>
    public static string SplashPhaseReady => Get(nameof(SplashPhaseReady));

    /// <summary>
    ///     启动台"模块加载失败"阶段名
    /// </summary>
    public static string SplashPhaseFailed => Get(nameof(SplashPhaseFailed));
}
