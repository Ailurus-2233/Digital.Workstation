using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace DigitalWorkstation.Core.UIPackage;

/// <summary>
///     VS Code Dark+ 色调调色板：覆盖 Semi 语义色键使整体色调对齐 VS Code，
///     并提供 chrome 专属色键（标题栏灰、状态栏蓝）。随应用固定 Dark 主题启用
/// </summary>
public static class VSCodePalette
{
    /// <summary>
    ///     把调色板写入应用级资源，使查找先于各主题包命中
    /// </summary>
    public static void ApplyTo(IResourceDictionary resources)
    {
        // Semi 语义色键覆盖（VS Code Dark+ 值）
        resources["SemiColorBackground0"] = Brush("#1F1F1F"); // 编辑器/MainContent 底色
        resources["SemiColorBackground1"] = Brush("#181818"); // ActivityBar/SideBar/面板底色
        resources["SemiColorBackground2"] = Brush("#252526"); // 弹出层等抬升表面
        resources["SemiColorBorder"] = Brush("#2B2B2B"); // 区域分隔线
        resources["SemiColorNavBackground"] = Brush("#181818");
        resources["SemiColorText0"] = Brush("#CCCCCC"); // 主要文本
        resources["SemiColorText1"] = Brush("#9D9D9D"); // 次要文本
        resources["SemiColorText2"] = Brush("#858585"); // 辅助文本（ActivityBar 未选中图标等）
        resources["SemiColorFill1"] = Brush("#2A2D2E"); // 悬停填充
        resources["SemiColorFill2"] = Brush("#37373D"); // 选中/按下填充
        resources["CaptionButtonForeground"] = Brush("#CCCCCC"); // 标题栏窗管按钮

        // chrome 专属色键
        resources["ChromeTitleBarBackground"] = Brush("#3C3C3C"); // 标题栏灰
        resources["ChromeStatusBarBackground"] = Brush("#007ACC"); // 状态栏蓝
        resources["ChromeStatusBarForeground"] = Brush("#FFFFFF");
        resources["ChromeActivityBarItemActiveForeground"] = Brush("#FFFFFF");

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
