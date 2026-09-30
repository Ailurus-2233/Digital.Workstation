namespace DigitalWorkstation.Core.Services.Plugins;

/// <summary>启动发现快照；禁用插件保留元数据，不加载程序集。</summary>
public sealed record PluginStartupInfo(string Id, string AssemblyPath, bool EnabledAtStartup,
    bool IsLoaded, string? Error)
{
    public string Name { get; init; } = Id;
}
