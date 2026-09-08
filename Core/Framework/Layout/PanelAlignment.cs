namespace DigitalWorkstation.Core.Framework.Layout;

/// <summary>
///     面板对齐：FrameworkWindow 基础布局的档位，决定 BottomPanel 在窗口底部的水平跨度
///     （领域术语见根目录 CONTEXT.md）；面板不占的列由侧栏通高到底吸收，不留空挡
/// </summary>
public enum PanelAlignment
{
    /// <summary>
    ///     左对齐：面板贴左，横跨 SideBar 与 MainContent 列下方；AuxiliaryPanel 通高到底
    /// </summary>
    Left,

    /// <summary>
    ///     右对齐：面板贴右，横跨 MainContent 与 AuxiliaryPanel 列下方；SideBar 通高到底
    /// </summary>
    Right,

    /// <summary>
    ///     居中：面板仅占 MainContent 列下方（默认）；SideBar 与 AuxiliaryPanel 通高到底
    /// </summary>
    Center,

    /// <summary>
    ///     两端对齐：面板横跨 SideBar/MainContent/AuxiliaryPanel 三列下方（无留白，全宽）
    /// </summary>
    Justify
}
