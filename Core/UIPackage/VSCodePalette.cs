using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace DigitalWorkstation.Core.UIPackage;

/// <summary>
///     产品深色调色板：以 VS Code Dark+ 的语义分层为基础覆盖 Semi 色键，
///     并提供 chrome 专属色键（状态栏蓝、ActivityBar 活动前景）。随应用固定 Dark 主题启用
/// </summary>
public static class VSCodePalette
{
    /// <summary>
    ///     把调色板写入应用级资源，使查找先于各主题包命中
    /// </summary>
    public static void ApplyTo(IResourceDictionary resources)
    {
        // Semi 语义色键覆盖（产品深色值）
        resources["SemiColorBackground0"] = Brush("#121314"); // MainContent 主面板底色
        resources["SemiColorBackground1"] = Brush("#191A1B"); // ActivityBar/SideBar/面板底色
        resources["SemiColorBackground2"] = Brush("#252526"); // 弹出层等抬升表面
        resources["SemiColorBorder"] = Brush("#2B2B2B"); // 区域分隔线
        resources["SemiColorNavBackground"] = Brush("#191A1B");
        resources["SemiColorText0"] = Brush("#CCCCCC"); // 主要文本
        resources["SemiColorText1"] = Brush("#9D9D9D"); // 次要文本
        resources["SemiColorText2"] = Brush("#858585"); // 辅助文本（ActivityBar 未选中图标等）
        resources["SemiColorFill1"] = Brush("#2A2D2E"); // 悬停填充
        resources["SemiColorFill2"] = Brush("#37373D"); // 选中/按下填充
        resources["CaptionButtonForeground"] = Brush("#CCCCCC"); // 标题栏窗管按钮

        // chrome 专属色键（标题栏直接使用 SemiColorNavBackground，与窗口背景同色）
        resources["ChromeStatusBarBackground"] = Brush("#3994BC"); // 状态栏蓝
        resources["ChromeStatusBarForeground"] = Brush("#FFFFFF");
        resources["ChromeActivityBarItemActiveForeground"] = Brush("#FFFFFF");
        resources["ChromeSashHoverBrush"] = Brush("#3994BC"); // 面板分隔条悬停/拖拽高亮（VS Code sash.hoverBorder）

        // 菜单弹出项密度对齐 VS Code：22px 行高（Semi 默认 16,8 内边距 + 14px 字 ≈ 33px）
        resources["MenuItemPadding"] = new Thickness(16, 1.5);
        resources["MenuFlyoutFontSize"] = 12.0;
        // 菜单弹出层配色对齐 VS Code：底 #1F1F1F、边线 #454545
        resources["MenuFlyoutBackground"] = Brush("#1F1F1F");
        resources["MenuFlyoutBorderBrush"] = Brush("#454545");
    }

    private static SolidColorBrush Brush(string color)
    {
        return new SolidColorBrush(Color.Parse(color));
    }
}
