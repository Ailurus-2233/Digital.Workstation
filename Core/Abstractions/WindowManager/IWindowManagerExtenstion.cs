using Avalonia.Controls;

namespace DigitalWorkstation.Core.Abstractions.WindowManager;

/// <summary>
/// 窗口管理器扩展方法，提供泛型版本的窗口管理操作
/// </summary>
public static class WindowManagerExtenstion {

    /// <summary>
    /// 获取指定类型的窗口实例
    /// </summary>
    /// <typeparam name="TWindow">
    /// 指定窗口类型
    /// </typeparam>
    /// <param name="manager">
    /// 窗口管理器实例
    /// </param>
    /// <returns>
    /// 从容器中解析得到的窗口实例
    /// </returns>
    public static Window? GetWindow<TWindow>(this IWindowManager manager) where TWindow : Window {
        return manager.GetWindow(typeof(TWindow));
    }
    
    /// <summary>
    /// 显示指定类型的对话框窗口
    /// </summary>
    /// <typeparam name="TWindow"></typeparam>
    /// <param name="manager"></param>
    public static void ShowWindow<TWindow>(this IWindowManager manager) where TWindow : Window {
        manager.ShowWindow(typeof(TWindow));
    }

    public static void ShowWindow<TWindow>(this IWindowManager manager, object dataContext) {
        manager.ShowWindow(typeof(TWindow), dataContext);
    }

    public static void ShowDialog<TWindow>(this IWindowManager manager) where TWindow : Window {
        manager.ShowDialog(typeof(TWindow));
    }

    public static void ShowDialog<TWindow>(this IWindowManager manager, object dataContext) {
        manager.ShowDialog(typeof(TWindow), dataContext);
    }

    public static void HideWindow<TWindow>(this IWindowManager manager) where TWindow : Window {
        manager.HideWindow(typeof(TWindow));
    }
    
    public static void CloseWindow<TWindow>(this IWindowManager manager) where TWindow : Window {
        manager.CloseWindow(typeof(TWindow));
    }
    
}