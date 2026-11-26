using Avalonia.Controls;
using Avalonia.Interactivity;
using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.Common.Events;

namespace DigitalWorkstation.Clash;

[ToolView("clash.nodes", typeof(ClashResources), "Title",
    Default = ToolViewPlacement.ActivityBar, Order = 20, Icon = "M2 4h7v6H2z M15 14h7v6h-7z M5 10v7h10")]
public sealed partial class ClashView : UserControl
{
    private readonly ClashService service;
    private readonly IEventAggregator events;
    private Subscription? editing;

    public ClashView() : this(DigitalWorkstation.Core.Common.IoC.Provider.Resolve<ClashService>(),
        DigitalWorkstation.Core.Common.IoC.Provider.Resolve<IEventAggregator>()) { }

    public ClashView(ClashService service, IEventAggregator events)
    {
        this.service = service;
        this.events = events;
        InitializeComponent();
        AttachedToVisualTree += (_, _) => { service.Changed += Update; Update(); };
        DetachedFromVisualTree += (_, _) => service.Changed -= Update;
    }

    private void Update()
    {
        AddButton.IsEnabled = Editor.IsEnabled = Cards.IsEnabled = !service.Busy;
        Power.IsChecked = service.Listening;
        Power.IsEnabled = !service.Busy && (service.Listening || service.Settings.Selected != null);
        ToolTip.SetTip(Power, service.Listening ? string.Format(ClashResources.Stop, service.Settings.Port) : ClashResources.Start);
        Cards.ItemsSource = service.Settings.Sources;
        RefreshButton.IsEnabled = !service.Busy && service.Settings.Sources.Length > 0;
        var updating = service.Operation == ClashOperation.UpdatingSubscriptions;
        SubscriptionDivider.IsVisible = !updating;
        SubscriptionProgress.IsIndeterminate = SubscriptionProgress.IsVisible = updating;
    }

    private void BeginEdit(Subscription? source)
    {
        EditorTitle.Text = source == null ? ClashResources.Add : ClashResources.Edit;
        editing = source;
        SourceName.Text = source?.Name ?? "";
        SourceUrl.Text = source?.Url ?? "";
        Editor.IsVisible = true;
        SourceName.Focus();
    }

    private async void Toggle(object? sender, RoutedEventArgs e) => await service.Run(service.Toggle);

    private void OpenNodes(object? sender, RoutedEventArgs e) =>
        events.GetEvent<OpenMainViewEvent>().Publish(ClashMainView.ViewId);

    private void Add(object? sender, RoutedEventArgs e) => BeginEdit(null);
    private void Edit(object? sender, RoutedEventArgs e) => BeginEdit((Subscription)((Button)sender!).DataContext!);
    private void Cancel(object? sender, RoutedEventArgs e) => Editor.IsVisible = false;
    private async void Save(object? sender, RoutedEventArgs e) => await service.Run(async () =>
    {
        await service.Import(SourceName.Text ?? "", SourceUrl.Text ?? "", editing);
        Editor.IsVisible = false;
    }, ClashOperation.UpdatingSubscriptions);
    private async void RefreshAll(object? sender, RoutedEventArgs e) =>
        await service.Run(service.RefreshAll, ClashOperation.UpdatingSubscriptions);
    private async void Refresh(object? sender, RoutedEventArgs e)
    {
        var source = (Subscription)((Button)sender!).DataContext!;
        await service.Run(() => service.Import(source.Name, source.Url, source), ClashOperation.UpdatingSubscriptions);
    }
    private async void Remove(object? sender, RoutedEventArgs e)
    {
        var source = (Subscription)((Button)sender!).DataContext!;
        await service.Run(async () =>
        {
            await service.Remove(source);
            if (editing?.Id == source.Id) Editor.IsVisible = false;
        });
    }
}
