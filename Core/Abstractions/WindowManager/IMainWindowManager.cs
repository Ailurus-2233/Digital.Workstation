using System;

namespace DigitalWorkstation.Core.Abstractions.WindowManager;

/// <summary>
/// 主窗口管理器接口
/// </summary>
public interface IMainWindowManager
{
    /// <summary>
    /// 处理主窗口的显示与隐藏
    /// </summary>
    void HandleMainWindow();

    /// <summary>
    /// 隐藏主窗口
    /// </summary>
    void HideMainWindow();

    /// <summary>
    /// 显示主窗口
    /// </summary>
    void ShowMainWindow();

    /// <summary>
    /// 关闭除主窗口外的所有窗口
    /// </summary>
    void CloseWindowsExceptMain();
}
