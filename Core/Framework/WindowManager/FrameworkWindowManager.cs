using Avalonia;
using Avalonia.Controls;
using DigitalWorkstation.Core.Abstractions.WindowManager;
using DigitalWorkstation.Core.Common;
using Prism.DryIoc;

namespace DigitalWorkstation.Core.Framework.WindowManager;

/// <summary>
/// 应用基本框架的窗口管理器实现，用于管理应用程序中的窗口显示与隐藏和主窗口操作
/// </summary>
public class FrameworkWindowManager : IWindowManager, IMainWindowManager
{
    /// <summary>
    /// 窗口类型与窗口实例映射表
    /// </summary>
    private readonly Dictionary<Type, Window> _windowMap = new();

    /// <summary>
    /// 主窗口实例
    /// </summary>
    private Window? _mainWindow;

    /// <summary>
    /// 主窗口为空时的错误消息
    /// </summary>
    private const string NullMainWindowError = "Main window is not set. Cannot show window.";

    /// <summary>
    /// 获取指定类型的窗口实例
    /// </summary>
    /// <param name="type">窗口类型</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Window GetWindow(Type type)
    {
        return IoC.Provider.Resolve(type) as Window ?? throw new ArgumentException("TargetWindow type is illegal");
    }

    /// <summary>
    /// 初始化窗口并注册关闭事件
    /// </summary>
    /// <param name="window">窗口实例</param>
    /// <exception cref="InvalidOperationException">当窗口类型已注册时抛出异常</exception>
    private void InitializeWindow(Window window)
    {
        var type = window.GetType();
        if (!_windowMap.TryAdd(type, window))
            throw new InvalidOperationException($"Window of type {type} is already registered.");
        window.Closing += (_, _) => { _windowMap.Remove(type); };
    }

    /// <summary>
    /// 显示指定类型的窗口
    /// </summary>
    /// <param name="type">窗口类型</param>
    public void ShowWindow(Type type)
    {
        ShowWindow(GetWindow(type));
    }

    /// <summary>
    /// 显示指定类型的窗口，并设置其数据上下文
    /// </summary>
    /// <param name="type">窗口类型</param>
    /// <param name="dataContext">数据上下文</param>
    public void ShowWindow(Type type, object dataContext)
    {
        var target = GetWindow(type);
        ShowWindow(target, dataContext);
    }

    /// <summary>
    /// 显示指定类型的窗口
    /// </summary>
    /// <param name="window">窗口实例</param>
    /// <exception cref="InvalidOperationException">当主窗口未设置时抛出异常</exception>
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

    /// <summary>
    /// 显示指定类型的窗口，并设置其数据上下文
    /// </summary>
    /// <param name="window">窗口实例</param>
    /// <param name="dataContext">数据上下文</param>
    /// <exception cref="InvalidOperationException">当主窗口未设置时抛出异常</exception>
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