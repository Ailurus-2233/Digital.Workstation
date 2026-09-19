using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.UIPackage;
using DigitalWorkstation.Sample.Resources;

namespace DigitalWorkstation.Sample;

/// <summary>
///     用于手动确认插件已加载且能向 Shell 提供贡献。
/// </summary>
public sealed class SampleStatusBarItem : IStatusBarItemContribution
{
    public string Id => "sample.status";

    public string Title => SampleResources.PluginLoaded;

    public string IconPath => Icons.Ready;

    public int Order => 30;
}
