using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.Abstractions.Plugins;
using DigitalWorkstation.Core.Abstractions.Network;
using DigitalWorkstation.Core.Framework.Contributions;
using DigitalWorkstation.Core.Framework.Settings;
using DigitalWorkstation.Core.Services.Contributions;
using DigitalWorkstation.Core.Services.Lifecycle;
using DigitalWorkstation.MailReader.Resources;

namespace DigitalWorkstation.MailReader;

[Plugin(MailPlugin.Id, typeof(MailResources), nameof(MailResources.PluginName))]
public sealed class MailPlugin : IModule
{
    public const string Id = "mail-reader";

    public void RegisterTypes(IContainerRegistry registry)
    {
        registry.RegisterSingleton<ICredentialStore, PlatformCredentialStore>();
        registry.RegisterShellContribution<ProxyModule>(_ => new ProxyModule(Id, MailResources.PluginName));
        registry.RegisterModuleService<MailService>();
        registry.RegisterSettings(typeof(MailPlugin).Assembly);
        registry.RegisterToolViews(typeof(MailPlugin).Assembly);
        registry.RegisterShellContribution<IMainViewContribution, MailMainView>();
        registry.Register<MailAccountView>();
        registry.Register<MailReadingView>();
    }
    public void OnInitialized(IContainerProvider provider) { }
}

public sealed class MailMainView : IMainViewContribution
{
    public const string ViewId = "mail-reader.message";
    public string Id => ViewId;
    public Type ViewType => typeof(MailReadingView);
    public string Title => MailResources.PluginName;
}
