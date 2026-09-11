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
    ///     顶层"文件"菜单的标题
    /// </summary>
    public static string MenuFileTitle => Get(nameof(MenuFileTitle));

    /// <summary>
    ///     顶层"视图"菜单的标题
    /// </summary>
    public static string MenuViewTitle => Get(nameof(MenuViewTitle));

    /// <summary>
    ///     顶层"帮助"菜单的标题
    /// </summary>
    public static string MenuHelpTitle => Get(nameof(MenuHelpTitle));

    /// <summary>
    ///     文件菜单"退出"项的标题
    /// </summary>
    public static string MenuExitTitle => Get(nameof(MenuExitTitle));

    /// <summary>
    ///     帮助菜单"关于"项的标题
    /// </summary>
    public static string MenuAboutTitle => Get(nameof(MenuAboutTitle));

    /// <summary>
    ///     SideBar 显隐切换项的标题（视图菜单）
    /// </summary>
    public static string ToggleSideBarTitle => Get(nameof(ToggleSideBarTitle));

    /// <summary>
    ///     BottomPanel 显隐切换项的标题（视图菜单）
    /// </summary>
    public static string ToggleBottomPanelTitle => Get(nameof(ToggleBottomPanelTitle));

    /// <summary>
    ///     AuxiliaryPanel 显隐切换项的标题（视图菜单）
    /// </summary>
    public static string ToggleAuxiliaryPanelTitle => Get(nameof(ToggleAuxiliaryPanelTitle));

    /// <summary>
    ///     面板对齐菜单"左对齐"项的标题
    /// </summary>
    public static string PanelAlignLeftTitle => Get(nameof(PanelAlignLeftTitle));

    /// <summary>
    ///     面板对齐菜单"右对齐"项的标题
    /// </summary>
    public static string PanelAlignRightTitle => Get(nameof(PanelAlignRightTitle));

    /// <summary>
    ///     面板对齐菜单"居中"项的标题
    /// </summary>
    public static string PanelAlignCenterTitle => Get(nameof(PanelAlignCenterTitle));

    /// <summary>
    ///     面板对齐菜单"两端对齐"项的标题
    /// </summary>
    public static string PanelAlignJustifyTitle => Get(nameof(PanelAlignJustifyTitle));

    /// <summary>
    ///     视图菜单"重置布局"项的标题
    /// </summary>
    public static string ResetLayoutTitle => Get(nameof(ResetLayoutTitle));

    /// <summary>
    ///     shell 预置状态栏"就绪"项的文本
    /// </summary>
    public static string StatusReadyTitle => Get(nameof(StatusReadyTitle));

    /// <summary>
    ///     DashBoard 贡献给文件菜单的"打开启动台"项的标题
    /// </summary>
    public static string DashBoardOpenWindowMenuTitle => Get(nameof(DashBoardOpenWindowMenuTitle));

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

    /// <summary>
    ///     命令面板输入框的水印（ADR-0005）
    /// </summary>
    public static string CommandPaletteWatermark => Get(nameof(CommandPaletteWatermark));

    /// <summary>
    ///     命令面板无匹配结果时的空态文案（ADR-0005）
    /// </summary>
    public static string NoMatchingCommands => Get(nameof(NoMatchingCommands));

    /// <summary>
    ///     设置页"常规"分组的显示名（Framework 预置设置分组，ADR-0006）
    /// </summary>
    public static string SettingsGeneralGroupName => Get(nameof(SettingsGeneralGroupName));

    /// <summary>
    ///     设置项"语言"的显示名（Framework 预置，ADR-0006）
    /// </summary>
    public static string SettingsLanguageName => Get(nameof(SettingsLanguageName));

    /// <summary>
    ///     语言设置项成员 ZhCN 的显示名（键按「设置项名称键 + 成员名」约定生成，ADR-0006 决策 8）
    /// </summary>
    public static string SettingsLanguageNameZhCN => Get(nameof(SettingsLanguageNameZhCN));

    /// <summary>
    ///     语言设置项成员 EnUS 的显示名（同上约定）
    /// </summary>
    public static string SettingsLanguageNameEnUS => Get(nameof(SettingsLanguageNameEnUS));

    /// <summary>
    ///     设置页需重启设置项被修改后的项级标记文本（ADR-0006 决策 7）
    /// </summary>
    public static string SettingsRestartPendingMark => Get(nameof(SettingsRestartPendingMark));

    /// <summary>
    ///     设置页顶部「存在未生效的需重启修改」横幅文本（ADR-0006 决策 7）
    /// </summary>
    public static string SettingsRestartBannerText => Get(nameof(SettingsRestartBannerText));

    /// <summary>
    ///     设置页重启横幅上「立即重启」按钮的标题（ADR-0006 决策 7）
    /// </summary>
    public static string SettingsRestartNowButtonTitle => Get(nameof(SettingsRestartNowButtonTitle));
}
