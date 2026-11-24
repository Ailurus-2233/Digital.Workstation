using Avalonia.Controls;
using Avalonia.Interactivity;
using DigitalWorkstation.Core.Abstractions.Contributions;

namespace DigitalWorkstation.Clash;

[ToolView("clash.nodes", typeof(ClashResources), "Title", MainViewId = ClashMainView.ViewId,
    Default = ToolViewPlacement.ActivityBar, Order = 20, Icon = "M2 4h7v6H2z M15 14h7v6h-7z M5 10v7h10")]
public sealed partial class ClashView : UserControl
{
    private readonly ClashService service;
    private Subscription? editing;

    public ClashView() : this(DigitalWorkstation.Core.Common.IoC.Provider.Resolve<ClashService>()) { }

    public ClashView(ClashService service)
    {
        this.service = service;
        InitializeComponent();
        AttachedToVisualTree += (_, _) => { service.Changed += Update; Update(); };
        DetachedFromVisualTree += (_, _) => service.Changed -= Update;
    }

    private void Update()
    {
        IsEnabled = !service.Busy;
        Power.IsChecked = service.Listening;
        Power.IsEnabled = service.Listening || service.Settings.Selected != null;
        ToolTip.SetTip(Power, service.Listening ? string.Format(ClashResources.Stop, service.Settings.Port) : ClashResources.Start);
        Cards.ItemsSource = service.Settings.Sources;
        RefreshButton.IsEnabled = service.Settings.Sources.Length > 0;
        Feedback.Text = service.Busy ? ClashResources.Busy : service.Message;
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

    private void Add(object? sender, RoutedEventArgs e) => BeginEdit(null);
    private void Edit(object? sender, RoutedEventArgs e) => BeginEdit((Subscription)((Button)sender!).DataContext!);
    private void Cancel(object? sender, RoutedEventArgs e) => Editor.IsVisible = false;
    private async void Save(object? sender, RoutedEventArgs e) => await service.Run(async () =>
    {
        await service.Import(SourceName.Text ?? "", SourceUrl.Text ?? "", editing);
        Editor.IsVisible = false;
    });
    private async void RefreshAll(object? sender, RoutedEventArgs e) => await service.Run(service.RefreshAll);
    private async void Refresh(object? sender, RoutedEventArgs e)
    {
        var source = (Subscription)((Button)sender!).DataContext!;
        await service.Run(() => service.Import(source.Name, source.Url, source));
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
