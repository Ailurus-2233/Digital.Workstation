using System.Collections.Concurrent;
using System.Globalization;
using System.Resources;

namespace DigitalWorkstation.Core.Resource;

/// <summary>
///     按资源所有者类型查找当前 UI 区域性的文案；资源清单以所有者全名命名并位于其程序集。
/// </summary>
public static class ResourceText
{
    private static readonly ConcurrentDictionary<Type, ResourceManager> Managers = new();

    /// <summary>
    ///     缺失翻译回退中性资源，缺键返回键本身；资源清单错误保持抛出。
    /// </summary>
    public static string Get(Type resourceType, string key)
    {
        var manager = Managers.GetOrAdd(resourceType, static type => new ResourceManager(type));
        return manager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    }
}
