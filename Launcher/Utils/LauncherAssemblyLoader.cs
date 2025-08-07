using System.Reflection;

namespace DigitalWorkstation.Launcher.Utils;

public static class LauncherAssemblyLoader
{
    private static readonly string _launcherFolder = Environment.CurrentDirectory;

    private static readonly string[] _targetFolders =
    [
        Path.Combine(_launcherFolder, "Core"),
        Path.Combine(_launcherFolder, "Libraries", "Serilog"),
    ];

    public static Assembly? FindCoreAssembly(object? sender, ResolveEventArgs args)
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
}