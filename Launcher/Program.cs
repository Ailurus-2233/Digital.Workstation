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

    public static AppBuilder BuildAvaloniaApp()
    {
        return Launcher.BuildAvaloniaApp();
    }
}