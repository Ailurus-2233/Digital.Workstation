namespace DigitalWorkstation.Core.Abstractions.Plugins;

/// <summary>标记插件入口；入口实现 Prism IModule，每个程序集只允许一个入口。</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class PluginAttribute : Attribute
{
    public PluginAttribute() { }
    public PluginAttribute(string key, Type resourceType, string nameKey)
    {
        ArgumentNullException.ThrowIfNull(resourceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(nameKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ResourceType = resourceType;
        NameKey = nameKey;
        Key = key;
    }
    /// <summary>名称资源所有者，须位于插件程序集；旧入口回退到入口全名。</summary>
    public Type? ResourceType { get; }
    /// <summary>显示名称的资源键。</summary>
    public string? NameKey { get; }
    /// <summary>稳定配置标识，不随显示名称更改。</summary>
    public string? Key { get; }
    /// <summary>显式内置模块名称；不支持插件间依赖。</summary>
    public string[] DependsOn { get; set; } = [];
}