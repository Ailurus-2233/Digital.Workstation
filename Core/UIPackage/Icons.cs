namespace DigitalWorkstation.Core.UIPackage;

/// <summary>
///     共享图标几何：StreamGeometry path 字符串，由 PathIcon 消费并随主题变色。
///     贡献类（导航项等）经本类引用图标，不在各自类中硬编码 path
/// </summary>
public static class Icons
{
    /// <summary>
    ///     设置齿轮，shell 预置"设置"导航项
    /// </summary>
    public const string Settings =
        "M12 15.5A3.5 3.5 0 0 1 8.5 12 3.5 3.5 0 0 1 12 8.5a3.5 3.5 0 0 1 3.5 3.5 3.5 3.5 0 0 1-3.5 3.5m7.43-2.53c.04-.32.07-.64.07-.97 0-.33-.03-.66-.07-1l2.11-1.63c.19-.15.24-.42.12-.64l-2-3.46c-.12-.22-.39-.31-.61-.22l-2.49 1c-.52-.39-1.06-.73-1.69-.98l-.37-2.65A.506.506 0 0 0 14 2h-4c-.25 0-.46.18-.5.42l-.37 2.65c-.63.25-1.17.59-1.69.98l-2.49-1c-.23-.09-.49 0-.61.22l-2 3.46c-.13.22-.07.49.12.64L4.57 11c-.04.34-.07.67-.07 1 0 .33.03.65.07.97l-2.11 1.66c-.19.15-.25.42-.12.64l2 3.46c.12.22.39.3.61.22l2.49-1.01c.52.4 1.06.74 1.69.99l.37 2.65c.04.24.25.42.5.42h4c.25 0 .46-.18.5-.42l.37-2.65c.63-.26 1.17-.59 1.69-.99l2.49 1.01c.22.08.49 0 .61-.22l2-3.46c.12-.22.07-.49-.12-.64l-2.11-1.66Z";

    /// <summary>
    ///     四宫格，DashBoard 启动台导航项
    /// </summary>
    public const string DashBoard = "M3 3h8v8H3V3m10 0h8v8h-8V3M3 13h8v8H3v-8m10 0h8v8h-8v-8Z";
    /// <summary>
    ///     向下箭头，BottomPanel 收起按钮
    /// </summary>
    public const string ChevronDown = "M7.41 8.58 12 13.17l4.59-4.59L18 10l-6 6-6-6 1.41-1.42Z";

    /// <summary>
    ///     向右箭头，AuxiliaryPanel 收起按钮
    /// </summary>
    public const string ChevronRight = "M8.59 16.58 13.17 12 8.59 7.41 10 6l6 6-6 6-1.41-1.42Z";
    /// <summary>
    ///     左侧面板，SideBar 显隐切换（视图菜单项）
    /// </summary>
    public const string PanelLeft = "M20 3H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V5a2 2 0 0 0-2-2m0 16H9V5h11v14Z";

    /// <summary>
    ///     底部面板，BottomPanel 显隐切换（视图菜单项）
    /// </summary>
    public const string PanelBottom = "M4 3h16a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2m0 2v9h16V5H4Z";

    /// <summary>
    ///     右侧面板，AuxiliaryPanel 显隐切换（视图菜单项）
    /// </summary>
    public const string PanelRight = "M4 3h16a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2m0 2v14h11V5H4Z";

    /// <summary>
    ///     关闭叉号，文件菜单"退出"项
    /// </summary>
    public const string Exit = "M19 6.41 17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12Z";

    /// <summary>
    ///     信息圆圈，帮助菜单"关于"项
    /// </summary>
    public const string About =
        "M11 9h2V7h-2m1 13c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8m0-18A10 10 0 0 0 2 12a10 10 0 0 0 10 10 10 10 0 0 0 10-10A10 10 0 0 0 12 2m-1 15h2v-6h-2v6Z";

    /// <summary>
    ///     勾选圆圈，shell 预置状态栏"就绪"项
    /// </summary>
    public const string Ready =
        "M12 2C6.5 2 2 6.5 2 12s4.5 10 10 10 10-4.5 10-10S17.5 2 12 2m0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8m4.59-12.42L10 14.17l-2.59-2.58L6 13l4 4 8-8-1.41-1.42Z";

    /// <summary>
    ///     左对齐横线组，面板对齐菜单"左对齐"项与对齐按钮的左对齐态
    /// </summary>
    public const string AlignLeft = "M3 3h18v2H3V3m0 4h12v2H3V7m0 4h18v2H3v-2m0 4h12v2H3v-2Z";

    /// <summary>
    ///     右对齐横线组，面板对齐菜单"右对齐"项与对齐按钮的右对齐态
    /// </summary>
    public const string AlignRight = "M3 3h18v2H3V3m6 4h12v2H9V7m-6 4h18v2H3v-2m6 4h12v2H9v-2Z";

    /// <summary>
    ///     居中横线组，面板对齐菜单"居中"项与对齐按钮的居中态
    /// </summary>
    public const string AlignCenter = "M3 3h18v2H3V3m4 4h10v2H7V7m-4 4h18v2H3v-2m4 4h10v2H7v-2Z";

    /// <summary>
    ///     两端对齐横线组，面板对齐菜单"两端对齐"项与对齐按钮的两端对齐态
    /// </summary>
    public const string AlignJustify = "M3 3h18v2H3V3m0 4h18v2H3V7m0 4h18v2H3v-2m0 4h18v2H3v-2Z";
}
