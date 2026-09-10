using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using DigitalWorkstation.Core.Abstractions.Commands;
using DigitalWorkstation.Core.Resource;

namespace DigitalWorkstation.Core.Framework.Windows;

/// <summary>
///     命令面板（ADR-0005）：窗口顶部居中的命令检索浮层。搜索框 + 列表 + 子串过滤（不区分大小写，
///     匹配本地化后标题）+ ↑↓/Enter/Esc 键盘导航 + MRU 内存置顶全部内聚在控件内（控件模式同
///     PanelResizer/ToolViewBar）；控件寿命 = 窗口寿命 = 应用寿命，内存 MRU 因此成立。
///     数据源经 ItemsSource 宽松绑定 ViewModel 的 Commands 集合（同 MenuBarItems 惯例），VM 零交互逻辑
/// </summary>
public class CommandPalette : Border
{
    public static readonly StyledProperty<IEnumerable<ICommandContribution>?> ItemsSourceProperty =
        AvaloniaProperty.Register<CommandPalette, IEnumerable<ICommandContribution>?>(nameof(ItemsSource));

    private readonly TextBox _input;
    private readonly ListBox _list;
    private readonly TextBlock _emptyState;
    /// <summary>
    ///     最近执行的命令 Id，新者在前；只在内存中，重启即清（ADR-0005 决策 8）
    /// </summary>
    private readonly List<string> _recentIds = [];
    private TopLevel? _topLevel;

    public CommandPalette()
    {
        IsVisible = false;
        _input = new TextBox
        {
            Classes = { "command-input" },
            Watermark = Language.CommandPaletteWatermark
        };
        _list = new ListBox
        {
            Classes = { "command-list" },
            MaxHeight = 320,
            ItemTemplate = new FuncDataTemplate<ICommandContribution>(BuildItem)
        };
        _emptyState = new TextBlock
        {
            Classes = { "placeholder" },
            Margin = new Thickness(0, 8, 0, 12),
            Text = Language.NoMatchingCommands
        };
        Grid.SetRow(_list, 1);
        Grid.SetRow(_emptyState, 1);
        Child = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            Children = { _input, _list, _emptyState }
        };
        _input.TextChanged += (_, _) => RefreshItems();
        // 单击条目即执行；点在滚动区空白（不在任何条目上）不触发
        _list.AddHandler(TappedEvent, OnItemTapped, RoutingStrategies.Bubble);
    }

    /// <summary>
    ///     命令数据源：全部命令贡献（收集时已按 Order/标题排序、Id 去重）
    /// </summary>
    public IEnumerable<ICommandContribution>? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    ///     打开面板：清空输入、重建列表、聚焦输入框，并开始监听面板外点击（失焦关闭）
    /// </summary>
    public void Open()
    {
        IsVisible = true;
        _input.Text = string.Empty;
        RefreshItems();
        _input.Focus();
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(PointerPressedEvent, OnOutsidePointerPressed, RoutingStrategies.Tunnel);
    }

    /// <summary>
    ///     关闭面板：Esc、面板外点击、执行命令后
    /// </summary>
    public void Close()
    {
        IsVisible = false;
        _topLevel?.RemoveHandler(PointerPressedEvent, OnOutsidePointerPressed);
        _topLevel = null;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == ItemsSourceProperty && IsVisible)
        {
            RefreshItems();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled)
        {
            return;
        }
        switch (e.Key)
        {
            case Key.Escape:
                Close();
                e.Handled = true;
                break;
            case Key.Enter:
                ExecuteSelected();
                e.Handled = true;
                break;
            case Key.Down:
                MoveSelection(1);
                e.Handled = true;
                break;
            case Key.Up:
                MoveSelection(-1);
                e.Handled = true;
                break;
        }
    }

    private static Control BuildItem(ICommandContribution? command, INameScope _)
    {
        // 虚拟化面板回收容器时以 null 调模板（ClearContainerForItemOverride → ContentPresenter 清空），
        // 第二次打开替换 ItemsSource 必走此路径
        if (command is null)
        {
            return new Grid();
        }

        var gesture = new TextBlock
        {
            Classes = { "gesture" },
            Text = command.Gesture,
            IsVisible = command.Gesture is not null
        };
        Grid.SetColumn(gesture, 1);
        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Children = { new TextBlock { Text = command.Title }, gesture }
        };
    }

    /// <summary>
    ///     子串过滤（不区分大小写，匹配本地化后标题）后 MRU 置顶：
    ///     最近执行的命令按先后浮到列表最前，其余保持收集时序
    /// </summary>
    private void RefreshItems()
    {
        var query = _input.Text?.Trim() ?? string.Empty;
        var matched = (ItemsSource ?? Enumerable.Empty<ICommandContribution>())
            .Where(command => query.Length == 0 ||
                              command.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var visible = new List<ICommandContribution>(matched.Length);
        var floated = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in _recentIds)
        {
            var hit = matched.FirstOrDefault(command => command.Id == id);
            if (hit is not null && floated.Add(id))
            {
                visible.Add(hit);
            }
        }
        visible.AddRange(matched.Where(command => !floated.Contains(command.Id)));
        _list.ItemsSource = visible;
        _list.SelectedIndex = visible.Count > 0 ? 0 : -1;
        _list.IsVisible = visible.Count > 0;
        _emptyState.IsVisible = visible.Count == 0;
    }

    private void MoveSelection(int delta)
    {
        if (_list.ItemsSource is not IReadOnlyList<ICommandContribution> { Count: > 0 } items)
        {
            return;
        }
        _list.SelectedIndex = Math.Clamp(_list.SelectedIndex + delta, 0, items.Count - 1);
        _list.ScrollIntoView(items[_list.SelectedIndex]);
    }

    private void OnItemTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Visual visual && visual.FindAncestorOfType<ListBoxItem>() is not null)
        {
            ExecuteSelected();
        }
    }

    private void OnOutsidePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Visual visual && this.IsVisualAncestorOf(visual))
        {
            return;
        }
        Close();
    }

    private void ExecuteSelected()
    {
        if (_list.SelectedItem is not ICommandContribution command)
        {
            return;
        }
        Close();
        _recentIds.Remove(command.Id);
        _recentIds.Insert(0, command.Id);
        command.Command.Execute(null);
    }
}
