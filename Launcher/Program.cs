// See https://aka.ms/new-console-template for more information

using Avalonia;

namespace DigitalWorkstation.Launcher;

public static class Program
{
    /// <summary>
    ///     程序入口，通过启动器引导核心框架
    /// </summary>
    /// <param name="args"></param>
    private static void Main(string[] args)
    {
        Launcher.Initialize();
        Launcher.Run(args);
    }

    /// <summary>
    ///     Avalonia 设计器/预览器入口：
    ///     设计时 AssemblyLoader.Initialize() 自动禁用，程序集直接从输出根目录探测。
    /// </summary>
    public static AppBuilder BuildAvaloniaApp() => Launcher.BuildAvaloniaApp();
}