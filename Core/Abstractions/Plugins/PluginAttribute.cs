namespace DigitalWorkstation.Core.Abstractions.Plugins;

/// <summary>
///     标记通过目录扫描发现的插件入口；入口类同时实现 Prism IModule。
///     每个插件程序集只能有一个入口，生命周期由宿主启动序列执行。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class PluginAttribute : Attribute
{
    /// <summary>依赖的显式内置模块名称，与宿主模块目录中的 ModuleName 一致；不支持插件间依赖。</summary>
    public string[] DependsOn { get; set; } = [];
}
