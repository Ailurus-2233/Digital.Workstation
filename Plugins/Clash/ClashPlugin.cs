using DigitalWorkstation.Core.Abstractions.Plugins;
using DigitalWorkstation.Core.Framework.Contributions;
using DigitalWorkstation.Core.Services.Lifecycle;
using DigitalWorkstation.Core.Framework.Settings;
using DigitalWorkstation.Core.Services.Contributions;
using DigitalWorkstation.Core.Abstractions.Contributions;

namespace DigitalWorkstation.Clash;

[Plugin("clash", typeof(ClashResources), "Title")]
public sealed class ClashPlugin : IModule
{
    public void RegisterTypes(IContainerRegistry registry)
    {
        registry.RegisterModuleService<ClashService>();
        registry.RegisterSettings(typeof(ClashPlugin).Assembly);
        registry.RegisterShellContribution<IMainViewContribution, ClashMainView>();
        registry.Register<NodeListView>();
        registry.Register<CoreSettingsPanel>();
        registry.RegisterToolViews(typeof(ClashPlugin).Assembly);
    }
    public void OnInitialized(IContainerProvider provider) { }
}




public sealed class ClashMainView : IMainViewContribution
{
    public const string ViewId = "clash.node-list";
    public string Id => ViewId;
    public Type ViewType => typeof(NodeListView);
}
