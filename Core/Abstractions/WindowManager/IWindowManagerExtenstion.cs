using Avalonia.Controls;

namespace DigitalWorkstation.Core.Abstractions.WindowManager;

public static class WindowManagerExtenstion {

    public static Window? GetWindow<TWindow>(this IWindowManager manager) where TWindow : Window {
        return manager.GetWindow(typeof(TWindow));
    }
    
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