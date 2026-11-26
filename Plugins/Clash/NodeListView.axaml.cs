using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DigitalWorkstation.Clash;

public sealed partial class NodeListView : UserControl
{
    private readonly ClashService service;
    private Subscription[]? sources;
    private IReadOnlyList<Node> displayedNodes = [];
    private string? activeNodeKey;
    private readonly List<Node> rows = [];
    private readonly DataGridCollectionView view;
    public NodeListView() : this(DigitalWorkstation.Core.Common.IoC.Provider.Resolve<ClashService>()) { }

    public NodeListView(ClashService service)
    {
        this.service = service;
        InitializeComponent();
        view = new DataGridCollectionView(rows);
        Nodes.ItemsSource = view;
        States.ItemsSource = new[] { new StateFilter(null) }.Concat(Enum.GetValues<NodeState>().Select(state => new StateFilter(state))).ToArray();
        States.SelectedIndex = 0;
        Query.TextChanged += (_, _) => Filter();
        Sources.SelectionChanged += (_, _) => Filter();
        States.SelectionChanged += (_, _) => Filter();

        AttachedToVisualTree += (_, _) => { service.Changed += Update; Update(); };
        DetachedFromVisualTree += (_, _) => service.Changed -= Update;
    }

    private void Update()
    {
        var settings = service.Settings;
        var selectedKey = activeNodeKey == settings.Selected ? (Nodes.SelectedItem as Node)?.Key : settings.Selected;
        activeNodeKey = settings.Selected;
        displayedNodes = service.Nodes;
        if (sources != settings.Sources)
        {
            var id = (Sources.SelectedItem as Subscription)?.Id;
            sources = settings.Sources;
            Sources.ItemsSource = new object[] { ClashResources.AllSources }.Concat(sources).ToArray();
            Sources.SelectedIndex = Array.FindIndex(sources, s => s.Id == id) + 1;
        }
        MeasureButton.IsEnabled = !service.Busy && service.Nodes.Count > 0;
        var measuring = service.Operation == ClashOperation.Measuring;
        MeasureIcon.IsVisible = !measuring;
        MeasureSpinner.IsVisible = measuring;
        Status.Text = service.Message;
        Status.IsVisible = !string.IsNullOrEmpty(Status.Text);
        Filter(selectedKey);
    }

    private void Filter() => Filter((Nodes.SelectedItem as Node)?.Key);

    private void Filter(string? selectedKey)
    {
        var term = Query.Text?.Trim() ?? "";
        var visible = displayedNodes.Where(n =>
            (Sources.SelectedItem is not Subscription source || n.Source.Id == source.Id) &&
            (States.SelectedItem is not StateFilter filter || filter.Matches(n)) &&
            (n.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || n.Source.Name.Contains(term, StringComparison.OrdinalIgnoreCase))).ToList();
        if (!rows.SequenceEqual(visible))
        {
            rows.Clear();
            rows.AddRange(visible);
            view.Refresh();
        }
        Nodes.SelectedItem = rows.FirstOrDefault(n => n.Key == selectedKey) ?? rows.FirstOrDefault(n => n.Key == activeNodeKey);
    }

    private async void Select(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: Node node })
            await service.Run(() => service.Select(node), ClashOperation.SelectingNode);
    }
    private async void Measure(object? sender, RoutedEventArgs e) =>
        await service.Run(service.Measure, ClashOperation.Measuring);
}
