namespace DigitalWorkstation.Common.Configuration;

internal class AssemblyConfiguration
{
    public string[] AssemblySearchPaths { get; init; } = [];

    /// <summary>
    ///     修改相对路径为绝对路径
    /// </summary>
    /// <param name="rootPath">
    ///     配置文件所在的文件夹
    /// </param>
    public void ToAbsolutePath(string rootPath)
    {
        for (var i = 0; i < AssemblySearchPaths.Length; i++)
        {
            if (IsAbsolutePath(AssemblySearchPaths[i]))
            {
                continue;
            }

            AssemblySearchPaths[i] = Path.Combine(rootPath, AssemblySearchPaths[i]);
            AssemblySearchPaths[i] = Path.GetFullPath(AssemblySearchPaths[i]);
        }
    }

    private bool IsAbsolutePath(string path)
    {
        return Path.IsPathRooted(path);
    }
}