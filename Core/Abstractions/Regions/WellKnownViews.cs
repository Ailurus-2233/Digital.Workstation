namespace DigitalWorkstation.Core.Abstractions.Regions;

/// <summary>
///     shell 与模块共同知晓的主视图 Id 常量（ADR-0006 决策 5）：
///     shell 侧导航按钮与贡献主视图的模块都引用本常量，从而互不依赖
/// </summary>
public static class WellKnownViews
{
    /// <summary>
    ///     设置页主视图：由 Settings 模块贡献（<c>IMainViewContribution</c>），
    ///     shell 左下角 Settings 按钮经 OpenMainViewEvent 以本 Id 打开
    /// </summary>
    public const string Settings = "settings.main";
}
