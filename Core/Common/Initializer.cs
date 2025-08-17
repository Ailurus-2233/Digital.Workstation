using DigitalWorkstation.Common.Tools;

namespace DigitalWorkstation.Common;

public static class Initializer
{
    public static void Initialize(string assemblyConfigPath = @".\Config\AssemblyPath.json")
    {
        AssemblyLoader.Initialize(assemblyConfigPath);
    }
}