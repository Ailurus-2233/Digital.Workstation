namespace DigitalWorkstation.Core.Framework.Shell;

/// <summary>
///     一次面板尺寸调整：目标区域 + 已换算方向的尺寸增量
/// </summary>
public readonly record struct PanelResize(PanelResizeTarget Target, double Delta);
