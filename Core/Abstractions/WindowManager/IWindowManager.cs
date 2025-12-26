using Avalonia.Controls;

namespace DigitalWorkstation.Core.Abstractions.WindowManager;

/// <summary>
/// 窗口管理器接口
/// <para>用于管理应用程序中的窗口显示与隐藏</para>
/// </summary>
public interface IWindowManager
{
    /// <summary>
    /// 获取指定类型的窗口实例
    /// </summary>
    /// <param name="type">
    /// 指定窗口类型
    /// </param>
    /// <returns>
    /// 从容器中解析得到的窗口实例
    /// </returns>
    Window GetWindow(Type type);

    /// <summary>
    /// 显示指定类型的窗口
    /// </summary>
    /// <param name="type">
    /// 指定窗口类型
    /// </param>
    void ShowWindow(Type type);

    /// <summary>
    /// 显示指定类型的窗口，并设置其数据上下文
    /// </summary>
    /// <param name="type">
    /// 指定窗口类型
    /// </param>
    /// <param name="dataContext">
    /// 窗口的数据上下文
    /// </param>
    void ShowWindow(Type type, object dataContext);

    /// <summary>
    /// 显示指定类型的对话框窗口
    /// </summary>
    /// <param name="window">
    /// 指定窗口实例
    /// </param>
    void ShowWindow(Window window);

    /// <summary>
    /// 显示指定类型的窗口，并设置其数据上下文
    /// </summary>
    /// <param name="window">
    /// 指定窗口实例
    /// </param>
    /// <param name="dataContext">
    /// 窗口的数据上下文
    /// </param>
    void ShowWindow(Window window, object dataContext);

    /// <summary>
    /// 显示指定类型的对话框窗口
    /// </summary>
    /// <param name="type">
    /// 指定Dialog窗口类型
    /// </param>
    void ShowDialog(Type type);

    /// <summary>
    /// 显示指定类型的对话框窗口，并设置其数据上下文
    /// </summary>
    /// <param name="type">
    /// 指定Dialog窗口类型
    /// </param>
    /// <param name="dataContext">
    /// 窗口的数据上下文
    /// </param>
    void ShowDialog(Type type, object dataContext);

    /// <summary>
    /// 显示指定类型的对话框窗口
    /// </summary>
    /// <param name="window">
    /// 指定窗口实例
    /// </param>
    void ShowDialog(Window window);

    /// <summary>
    /// 显示指定类型的对话框窗口，并设置其数据上下文
    /// </summary>
    /// <param name="window">
    /// 指定窗口实例
    /// </param>
    /// <param name="dataContext">
    /// 窗口的数据上下文
    /// </param>
    void ShowDialog(Window window, object dataContext);

    /// <summary>
    /// 关闭指定类型的窗口
    /// </summary>
    /// <param name="type">
    /// 指定窗口类型
    /// </param>
    void CloseWindow(Type type);

    /// <summary>
    /// 隐藏指定类型的窗口
    /// </summary>
    /// <param name="type">
    /// 指定窗口类型
    /// </param>
    void HideWindow(Type type);
}