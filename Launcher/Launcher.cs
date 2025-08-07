using Avalonia;
using DigitalWorkstation.Common;
using DigitalWorkstation.Framework;
using DigitalWorkstation.Launcher.Utils;

namespace DigitalWorkstation.Launcher;

public static class Launcher
{
    /// <summary>
    ///     初始化启动器流程
    /// </summary>
    public static void Initialize()
    {
        ConfigAssemblyResolve();
    }
    
    /// <summary>
    ///     启动器运行主函数，启动核心程序
    /// </summary>
    /// <param name="args">
    ///     运行参数
    /// </param>
    public static void Run(string[] args)
    {
        Logger.Information("Application startup.", nameof(Launcher));
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    ///     配置核心程序集的搜索路径和加载逻辑
    /// </summary>
    private static void ConfigAssemblyResolve()
    {
        AppDomain.CurrentDomain.AssemblyResolve += LauncherAssemblyLoader.FindCoreAssembly;
        AssemblyLoader.Initialize(@".\Config\AssemblyPath.json");
        AppDomain.CurrentDomain.AssemblyResolve -= LauncherAssemblyLoader.FindCoreAssembly;
        AppDomain.CurrentDomain.AssemblyResolve += AssemblyLoader.Instance.ResolveAssembly;
    }
    
    /// <summary>
    ///    TODO： 临时测试能否启动 Avalonia 应用程序，后续需要修改使用 Prism 启动
    /// </summary>
    /// <returns></returns>
    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
    
    public static AppBuilder BuildAvaloniaAppPreview()
    {
        Initialize();
        return BuildAvaloniaApp();
    }
}