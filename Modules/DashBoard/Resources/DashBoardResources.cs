using DigitalWorkstation.Core.Resource;

namespace DigitalWorkstation.DashBoard.Resources;

/// <summary>
///     所属模块的界面文案；中性资源为中文，en-US 为英文。
/// </summary>
public static class DashBoardResources
{
    /// <summary>
    ///     DashBoard 导航项的标题
    /// </summary>
    public static string DashBoardNavigationTitle => ResourceText.Get(typeof(DashBoardResources), nameof(DashBoardNavigationTitle));

    /// <summary>
    ///     启动台显示进度前的初始阶段文本
    /// </summary>
    public static string SplashStartingText => ResourceText.Get(typeof(DashBoardResources), nameof(SplashStartingText));

    /// <summary>
    ///     启动台"初始化核心服务"阶段名
    /// </summary>
    public static string SplashPhaseCoreServices => ResourceText.Get(typeof(DashBoardResources), nameof(SplashPhaseCoreServices));

    /// <summary>
    ///     启动台"加载模块"阶段名
    /// </summary>
    public static string SplashPhaseLoadingModules => ResourceText.Get(typeof(DashBoardResources), nameof(SplashPhaseLoadingModules));

    /// <summary>
    ///     启动台"就绪"阶段名
    /// </summary>
    public static string SplashPhaseReady => ResourceText.Get(typeof(DashBoardResources), nameof(SplashPhaseReady));

    /// <summary>
    ///     启动台"模块加载失败"阶段名
    /// </summary>
    public static string SplashPhaseFailed => ResourceText.Get(typeof(DashBoardResources), nameof(SplashPhaseFailed));
}
