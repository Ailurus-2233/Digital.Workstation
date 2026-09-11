using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.Abstractions.Regions;
using DigitalWorkstation.Settings.Views;

namespace DigitalWorkstation.Settings;

/// <summary>
///     设置页主视图贡献：Id 取自契约层 <see cref="WellKnownViews.Settings" />，
///     shell 左下角按钮与本模块经该常量对接而互不依赖（ADR-0006 决策 5）
/// </summary>
public class SettingsMainView : IMainViewContribution
{
    public string Id => WellKnownViews.Settings;

    public Type ViewType => typeof(SettingsPageView);
}
