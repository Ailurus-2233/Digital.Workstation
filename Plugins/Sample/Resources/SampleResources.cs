using DigitalWorkstation.Core.Resource;

namespace DigitalWorkstation.Sample.Resources;

/// <summary>
///     插件私有界面文案；中性资源为中文，en-US 为英文。
/// </summary>
public static class SampleResources
{
    public static string PluginLoaded => ResourceText.Get(typeof(SampleResources), nameof(PluginLoaded));
}
