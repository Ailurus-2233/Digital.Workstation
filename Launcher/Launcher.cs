using Avalonia;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Workstation;

namespace DigitalWorkstation.Launcher;

/// <summary>
///     程序的启动器工具：
///     1. 指定程序运行需要基础的程序集路径
///     2. 启动 Common 程序集初始化
///     3. 指定待启动的 Avalonia Application
///     4. 启动 Prism 应用框架
///     5. 结束初始化
/// </summary>
public static class Launcher
{
    /// <summary>
    ///     初始化启动器流程
    /// </summary>
    public static void Initialize()
    {
        AssemblyLoader.Initialize();
        InitializeCore();
    }

    private static void InitializeCore()
    {
        // TODO 添加初始化逻辑
    }


    /// <summary>
    ///     启动器运行主函数，启动核心程序
    /// </summary>
    /// <param name="args">
    ///     运行参数
    /// </param>
    [STAThread]
    public static void Run(string[] args)
    {
        Logger.Information("Application startup.", nameof(Launcher));
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<WorkstationApplication>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}