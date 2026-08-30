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
    ///     滑杆，shell 预置"属性"面板 tab
    /// </summary>
    public const string Properties =
        "M3 17v2h6v-2H3M3 5v2h10V5H3m10 16v-2h8v-2h-8v-2h-2v6h2M7 9v2H3v2h4v2h2V9H7m14 4v-2H11v2h10m-6-4h2V7h4V5h-4V3h-2v6h2Z";

    /// <summary>
    ///     层级列表，shell 预置"大纲"面板 tab
    /// </summary>
    public const string Outline =
        "M5 9.5 7.5 14h-5L5 9.5M3 4h4v4H3V4m2 16a2 2 0 0 0 2-2 2 2 0 0 0-2-2 2 2 0 0 0-2 2 2 2 0 0 0 2 2m4-15v2h12V5H9m0 14h12v-2H9v2m0-6h12v-2H9v2Z";

    /// <summary>
    ///     终端，shell 预置"输出"面板 tab
    /// </summary>
    public const string Output =
        "M20 19V7H4v12h16M20 3a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h16m-7 14v-2h5v2h-5M9.58 13 5.57 9h2.83l3.3 3.3c.39.39.39 1.03 0 1.42L8.42 17H5.59L9.58 13Z";

    /// <summary>
    ///     文本文件，shell 预置"日志"面板 tab
    /// </summary>
    public const string Log =
        "M6 2a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8l-6-6H6m0 2h7v5h5v11H6V4m2 8v2h8v-2H8m0 4v2h5v-2H8Z";

    /// <summary>
    ///     勾选清单，DashBoard"任务"面板 tab
    /// </summary>
    public const string Tasks =
        "M19 3h-4.18C14.4 1.84 13.3 1 12 1c-1.3 0-2.4.84-2.82 2H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V5a2 2 0 0 0-2-2m-7 0a1 1 0 0 1 1 1 1 1 0 0 1-1 1 1 1 0 0 1-1-1 1 1 0 0 1 1-1M7 7h10V5h2v14H5V5h2v2m.5 6.5L9 12l2 2 4.5-4.5L17 11l-6 6-3.5-3.5Z";
    /// <summary>
    ///     向下箭头，BottomPanel 收起按钮
    /// </summary>
    public const string ChevronDown = "M7.41 8.58 12 13.17l4.59-4.59L18 10l-6 6-6-6 1.41-1.42Z";

    /// <summary>
    ///     向右箭头，AuxiliaryPanel 收起按钮
    /// </summary>
    public const string ChevronRight = "M8.59 16.58 13.17 12 8.59 7.41 10 6l6 6-6 6-1.41-1.42Z";
    /// <summary>
    ///     左侧面板，SideBar 显隐切换（菜单项与快速工具栏按钮）
    /// </summary>
    public const string PanelLeft = "M20 3H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2V5a2 2 0 0 0-2-2m0 16H9V5h11v14Z";

    /// <summary>
    ///     底部面板，BottomPanel 显隐切换（菜单项与快速工具栏按钮）
    /// </summary>
    public const string PanelBottom = "M4 3h16a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2m0 2v9h16V5H4Z";

    /// <summary>
    ///     右侧面板，AuxiliaryPanel 显隐切换（菜单项与快速工具栏按钮）
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
}
