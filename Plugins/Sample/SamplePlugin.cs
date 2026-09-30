using DigitalWorkstation.Sample.Resources;
using DigitalWorkstation.Core.Services.Lifecycle;
using DigitalWorkstation.Core.Services.Contributions;
using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.Abstractions.Plugins;
using DigitalWorkstation.Core.Framework.Contributions;

namespace DigitalWorkstation.Sample;

/// <summary>
///     通过程序集扫描加载的插件入口，复用模块的注册与初始化生命周期。
/// </summary>
[Plugin("sample", typeof(SampleResources), nameof(SampleResources.PluginName))]
public sealed class SamplePlugin : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterModuleService<SampleModuleService>();
        containerRegistry.RegisterShellContribution<IStatusBarItemContribution, SampleStatusBarItem>();
    }

    public void OnInitialized(IContainerProvider containerProvider) { }
}