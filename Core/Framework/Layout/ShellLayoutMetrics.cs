using Avalonia;
using Avalonia.Controls;

namespace DigitalWorkstation.Core.Framework.Layout;

/// <summary>
///     卡片模板与列宽投影共用尺寸来源，避免 Shell 再解释主题的外边距。
/// </summary>
public static class ShellLayoutMetrics
{
    private const double Inset = 2;
    public static Thickness CardMargin { get; } = new(Inset);
    public static Thickness ContainerPadding { get; } = new(Inset);
    public static Thickness ActivityBarMargin { get; } = new(0, 0, Inset, 0);

    public static GridLength PanelColumn(double contentWidth, bool visible) =>
        new(visible ? contentWidth + CardMargin.Left + CardMargin.Right : 0);
}
