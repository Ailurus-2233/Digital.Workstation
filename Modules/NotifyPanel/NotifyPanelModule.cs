using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.Services.Contributions;
using DigitalWorkstation.Core.UIPackage;

namespace DigitalWorkstation.NotifyPanel;

public sealed class NotifyPanelModule : IModule
{
    public void RegisterTypes(IContainerRegistry registry)
    {
        registry.Register<NotifyPanelView>();
        registry.RegisterShellContribution<NotificationBubble, NotificationBubble>();
        registry.RegisterSingleton<NotifyPanelViewModel>();
        registry.RegisterShellContribution<ToolViewContribution>(provider => new ToolViewContribution
        {
            Id = "notify-panel.notifications",
            Title = NotifyResources.Title,
            IconPath = Icons.Bell,
            Badge = provider.Resolve<NotifyPanelViewModel>(),
            Placement = ToolViewPlacement.AuxiliaryPanel,
            AllowMove = true,
            Order = 10,
            ViewType = typeof(NotifyPanelView)
        });
    }

    public void OnInitialized(IContainerProvider provider) { }
}
