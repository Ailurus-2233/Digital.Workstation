using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using DigitalWorkstation.Common;
using DigitalWorkstation.WindowManager;
using Prism.DryIoc;

namespace DigitalWorkstation.Framework.WindowManager;

public class FrameworkWindowManager : IWindowManager
{
    private readonly Dictionary<Type, Window> _windowMap = new();
    private Window? _mainWindow;
    private const string NullMainWindowError = "Main window is not set. Cannot show window.";

    public Window GetWindow(Type type)
    {
        return IoC.Provider.Resolve(type) as Window ?? throw new ArgumentException("TargetWindow type is illegal");
    }

    private void InitializeWindow(Window window)
    {
        var type = window.GetType();
        if (!_windowMap.TryAdd(type, window))
            throw new InvalidOperationException($"Window of type {type} is already registered.");
        window.Closing += (_, _) => { _windowMap.Remove(type); };
    }

    public void ShowWindow(Type type)
    {
        ShowWindow(GetWindow(type));
    }

    public void ShowWindow(Type type, object dataContext)
    {
        var target = GetWindow(type);
        ShowWindow(target, dataContext);
    }

    public void ShowWindow(Window window)
    {
        InitializeWindow(window);
        if (_mainWindow != null)
        {
            if (_mainWindow.IsActive)
                window.Show(_mainWindow);
            else
                window.Show();
        }
        else
            throw new InvalidOperationException(NullMainWindowError);
    }

    public void ShowWindow(Window window, object dataContext)
    {
        InitializeWindow(window);
        window.DataContext = dataContext;
        if (_mainWindow != null)
        {
            if (_mainWindow.IsActive)
                window.Show(_mainWindow);
            else
                window.Show();
        }
        else
            throw new InvalidOperationException(NullMainWindowError);
    }

    public void ShowDialog(Type type)
    {
        ShowDialog(GetWindow(type));
    }

    public void ShowDialog(Type type, object dataContext)
    {
        var target = GetWindow(type);
        ShowDialog(target, dataContext);
    }

    public void ShowDialog(Window window)
    {
        InitializeWindow(window);
        if (_mainWindow is { IsActive: true })
            window.ShowDialog(_mainWindow);
        else
            throw new InvalidOperationException(NullMainWindowError);
    }

    public void ShowDialog(Window window, object dataContext)
    {
        InitializeWindow(window);
        window.DataContext = dataContext;
        if (_mainWindow is { IsActive: true })
            window.ShowDialog(_mainWindow);
        else
            throw new InvalidOperationException(NullMainWindowError);
    }


    public void CloseWindow(Type type)
    {
        if (_windowMap.TryGetValue(type, out var target))
            target.Close();
    }

    public void HideWindow(Type type)
    {
        if (_windowMap.TryGetValue(type, out var target))
            target.Hide();
        else
            throw new KeyNotFoundException($"No window of type {type} is currently open.");
    }

    public void HandleMainWindow()
    {
        var app = Application.Current as PrismApplication;
        var currentMainWindow = app?.MainWindow;
        _mainWindow = currentMainWindow as Window;
        if (_mainWindow != null && !_windowMap.ContainsKey(_mainWindow.GetType()))
            InitializeWindow(_mainWindow);
        else
            throw new InvalidOperationException(NullMainWindowError);
    }

    public void HideMainWindow() => _mainWindow?.Hide();
    public void ShowMainWindow() => _mainWindow?.Show();

    public void CloseWindowsExceptMain()
    {
        var windowsToClose = _windowMap.Values.Where(w => w != _mainWindow).ToList();
        foreach (var window in windowsToClose)
            window.Close();
    }
}