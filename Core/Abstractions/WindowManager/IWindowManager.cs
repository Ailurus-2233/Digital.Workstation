using Avalonia.Controls;

namespace DigitalWorkstation.WindowManager;

public interface IWindowManager
{
    Window GetWindow(Type type);
    void ShowWindow(Type type);
    void ShowWindow(Type type, object dataContext);
    void ShowWindow(Window window);
    void ShowWindow(Window window, object dataContext);
    void ShowDialog(Type type);
    void ShowDialog(Type type, object dataContext);
    void ShowDialog(Window window);
    void ShowDialog(Window window, object dataContext);
    void CloseWindow(Type type);
    void HideWindow(Type type);
    void HandleMainWindow();
    void HideMainWindow();
    void ShowMainWindow();
    void CloseWindowsExceptMain();
}