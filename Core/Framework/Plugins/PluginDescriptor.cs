namespace DigitalWorkstation.Core.Framework.Plugins;

/// <summary>发现结果保留错误归属，由启动序列统一呈现进度并决定继续或退出。</summary>
internal sealed record PluginDescriptor(
    string Name,
    string AssemblyPath,
    Type? ModuleType,
    IReadOnlyList<string> Dependencies,
    Exception? Error);
