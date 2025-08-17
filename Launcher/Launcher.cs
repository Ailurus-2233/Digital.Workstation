using System.Reflection;
using DigitalWorkstation.Common.Tools;
using DigitalWorkstation.Workstation;

namespace DigitalWorkstation.Launcher;

/// <summary>
///     程序的启动器工具：
///         1. 指定程序运行需要基础的程序集路径
///         2. 启动 Common 程序集初始化
///         3. 指定待启动的 Avalonia Application
///         4. 启动 Prism 应用框架
///         5. 结束初始化
/// </summary>
public static class Launcher
{
    
    /// <summary>
    ///     初始化启动器流程
    /// </summary>
    public static void Initialize()
    {
        AppDomain.CurrentDomain.AssemblyResolve += FindCoreAssembly;
        CommonInitialize();
        AppDomain.CurrentDomain.AssemblyResolve -= FindCoreAssembly;
        AppDomain.CurrentDomain.AssemblyResolve += AssemblyLoader.Instance.ResolveAssembly;
    }
    
    /// <summary>
    ///     Common 程序集初始化
    /// </summary>
    private static void CommonInitialize()
    {
        Common.Initializer.Initialize();
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
        Starter.Run(args);
    }

    
    #region Assembly Resolve
    
    
    private static readonly string _launcherFolder = Environment.CurrentDirectory;

    private static readonly string[] _targetFolders =
    [
        Path.Combine(_launcherFolder, "Core"),
        Path.Combine(_launcherFolder, "Libraries", "Serilog"),
    ];
    
    private static Assembly? FindCoreAssembly(object? sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name;
        
        if (string.IsNullOrWhiteSpace(assemblyName))
            return null;
        
        var folder = assemblyName.Split('.')[0];
        var targetFolder = _targetFolders.FirstOrDefault(x => x.Contains(folder));
        
        if (targetFolder != null)
        {
            var result = LoadAssembly(targetFolder, assemblyName);
            if (result != null)
            {
                return result;
            }
        }
        
        foreach (var dir in _targetFolders)
        {
            var result = LoadAssembly(dir, assemblyName);
            if (result != null)
            {
                return result;
            }
        }

        throw new FileNotFoundException($"Assembly {args.Name} not found.");
    }
    
    private static Assembly? LoadAssembly(string path, string assemblyName)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            if (!Directory.Exists(fullPath))
                return null;

            var extensions = new[] { ".dll", ".exe" };
            return (from ext in extensions
                select Path.Combine(fullPath, $"{assemblyName}{ext}")
                into assemblyPath
                where File.Exists(assemblyPath)
                select Assembly.LoadFrom(assemblyPath)).FirstOrDefault();
        }
        catch (Exception)
        {
            return null;
        }
    }

    #endregion
}