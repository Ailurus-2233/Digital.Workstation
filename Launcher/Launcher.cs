using System.Runtime.CompilerServices;
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
        // Release 下 native 库被归入 runtimes/<rid>/native/，默认 probing 找不到，
        // 注册 DllImportResolver 并立即预加载，让后续 P/Invoke 命中已加载的 handle。
        // 此处只引用 Launcher 自身程序集，不会触发 Avalonia/SkiaSharp 加载。
        AssemblyLoader.RegisterNativeResolvers();
        AssemblyLoader.PreloadNativeLibraries();
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
        RunAvalonia(args);
    }

    /// <summary>
    ///     独立方法启动 Avalonia：
    ///     保证 Avalonia 类型的 JIT 延迟到 AssemblyLoader.Initialize() 之后，
    ///     此时引导程序集已预加载、AssemblyResolve 已注册。
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunAvalonia(string[] args)
    {
        var builder = BuildAvaloniaApp();
        // AppBuilder 构造完毕说明 Avalonia 程序集已全部加载完毕，
        // 此时引用 SkiaSharp/HarfBuzzSharp 类型是安全的，为它们注册 native 解析器。
        AssemblyLoader.RegisterNativeResolversForAvalonia();
        builder.StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<WorkstationApplication>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .WithDeveloperTools();
    }
}