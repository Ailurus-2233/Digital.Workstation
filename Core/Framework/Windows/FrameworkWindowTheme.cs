using Avalonia.Markup.Xaml.Styling;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Styling;

namespace DigitalWorkstation.Core.Framework.Windows;

/// <summary>
///     FrameworkWindow 的基础布局主题：四份静态布局模板（WindowLayout* 资源）+ 五区 shell 的样式。
///     布局模板整体切换，不做动态调整。
///     经 StyleInclude 从编译进程序集的 axaml 资源加载（与 Semi/Ursa 主题同款机制）；
///     构造时强制 Loaded，保证窗口构造期即可查到布局模板资源
/// </summary>
public class FrameworkWindowTheme : Styles
{
    private static readonly Uri BaseUri = new("avares://DigitalWorkstation.Core.Framework/Windows/");

    /// <summary>
    ///     供标题栏菜单样式使用：锚定矩形不得跨出菜单按钮中心所在屏幕。
    /// </summary>
    public static CustomPopupPlacementCallback MenuPopupPlacement { get; } = placement =>
    {
        placement.Anchor = PopupAnchor.BottomLeft;
        placement.Gravity = PopupGravity.BottomRight;
        if (TopLevel.GetTopLevel(placement.Target) is not { } topLevel ||
            topLevel.Screens?.ScreenFromPoint(topLevel.PointToScreen(placement.AnchorRectangle.Center)) is not { } screen)
        {
            return;
        }

        // 最大化 chrome 的边缘可能落在相邻屏幕；原生定位器按锚定矩形左上角选屏，而非按钮中心。
        var screenBounds = new Rect(
            topLevel.PointToClient(screen.Bounds.Position),
            topLevel.PointToClient(screen.Bounds.BottomRight));
        placement.AnchorRectangle = placement.AnchorRectangle.Intersect(screenBounds);
    };

    public FrameworkWindowTheme()
    {
        var include = new StyleInclude(BaseUri)
        {
            Source = new Uri("FrameworkWindowTheme.axaml", UriKind.Relative)
        };
        _ = include.Loaded;
        Add(include);
    }
}
