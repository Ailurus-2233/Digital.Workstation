using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using DigitalWorkstation.Core.Abstractions.Contributions;

namespace DigitalWorkstation.Core.Framework.Layout;

/// <summary>
///     工具视图 Bar 的投放目标（ADR-0002）：接受 <see cref="ToolViewButton" /> 发起的拖拽。
///     DragOver 按指针位置计算插入序号，并把模板中的 PART_InsertionLine 占位线移动到落点缝隙；
///     Drop 把落点包装为 <see cref="ToolViewMove" /> 执行 MoveCommand。控件模式仿
///     <see cref="PanelResizer" />：控件只做手势与视觉，状态转换在 ViewModel；
///     ActivityBar 底部段不使用本控件，自然拒绝拖放
/// </summary>
public class ToolViewBar : ItemsControl
{
    private const string DragOverClass = "drag-over";

    /// <summary>
    ///     占位线厚度（逻辑像素）
    /// </summary>
    private const double InsertionLineThickness = 2;

    public static readonly StyledProperty<ToolViewPlacement> TargetBarProperty =
        AvaloniaProperty.Register<ToolViewBar, ToolViewPlacement>(nameof(TargetBar));

    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<ToolViewBar, Orientation>(nameof(Orientation));

    public static readonly StyledProperty<ICommand?> MoveCommandProperty =
        AvaloniaProperty.Register<ToolViewBar, ICommand?>(nameof(MoveCommand));

    private Border? _insertionLine;

    public ToolViewBar()
    {
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
    }

    /// <summary>
    ///     本 Bar 的身份：Drop 时随 ToolViewMove 报告给 ViewModel
    /// </summary>
    public ToolViewPlacement TargetBar
    {
        get => GetValue(TargetBarProperty);
        set => SetValue(TargetBarProperty, value);
    }

    /// <summary>
    ///     条目排列方向：决定插入序号按 X 还是 Y 计算、占位线横向还是纵向
    /// </summary>
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ToolViewDragSession.ActiveChanged += OnDragSessionChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ToolViewDragSession.ActiveChanged -= OnDragSessionChanged;
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>
    ///     会话结束兜底：Esc 取消或在窗口外松手等路径下，最后悬停的 Bar 收不到 DragLeave，
    ///     仅靠 Drop/DragLeave 清除会残留高亮
    /// </summary>
    private void OnDragSessionChanged(bool active)
    {
        if (!active)
        {
            ClearInsertion();
        }
    }

    /// <summary>
    ///     落放出口：ViewModel 的 MoveTabCommand
    /// </summary>
    public ICommand? MoveCommand
    {
        get => GetValue(MoveCommandProperty);
        set => SetValue(MoveCommandProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _insertionLine = e.NameScope.Find<Border>("PART_InsertionLine");
        if (_insertionLine is null)
        {
            return;
        }

        // 占位线方向：横向 Bar（面板 tab 条）为竖线，纵向 Bar（ActivityBar）为横线
        if (Orientation == Orientation.Horizontal)
        {
            _insertionLine.Width = InsertionLineThickness;
            _insertionLine.HorizontalAlignment = HorizontalAlignment.Left;
            _insertionLine.VerticalAlignment = VerticalAlignment.Stretch;
        }
        else
        {
            _insertionLine.Height = InsertionLineThickness;
            _insertionLine.HorizontalAlignment = HorizontalAlignment.Stretch;
            _insertionLine.VerticalAlignment = VerticalAlignment.Top;
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (!e.DataTransfer.Contains(ToolViewDragSession.TabIdFormat))
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
        ShowInsertion(ComputeInsertionIndex(e));
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var index = ComputeInsertionIndex(e);
        ClearInsertion();
        if (e.DataTransfer.TryGetValue(ToolViewDragSession.TabIdFormat) is { } tabId)
        {
            MoveCommand?.Execute(new ToolViewMove(tabId, TargetBar, index));
        }

        e.Handled = true;
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        // DragLeave 是冒泡事件：指针在条目间移动时会从子按钮冒泡上来，只在真正离开本 Bar 时清除
        if (!Bounds.Contains(e.GetPosition(this)))
        {
            ClearInsertion();
        }
    }

    /// <summary>
    ///     指针在某条目前半 → 插到该条目之前；所有条目后半之后 → 追加到末尾
    /// </summary>
    private int ComputeInsertionIndex(DragEventArgs e)
    {
        for (var i = 0; i < Items.Count; i++)
        {
            if (ContainerFromIndex(i) is not { } container)
            {
                continue;
            }

            var position = e.GetPosition(container);
            var before = Orientation == Orientation.Horizontal
                ? position.X < container.Bounds.Width / 2
                : position.Y < container.Bounds.Height / 2;
            if (before)
            {
                return i;
            }
        }

        return Items.Count;
    }

    /// <summary>
    ///     把占位线移到落点缝隙：落点前有条目取该条目左/上缘，落点在末尾取末条目右/下缘，
    ///     空 Bar 落在起点
    /// </summary>
    private void ShowInsertion(int index)
    {
        Classes.Add(DragOverClass);
        if (_insertionLine is null)
        {
            return;
        }

        var horizontal = Orientation == Orientation.Horizontal;
        var offset = 0.0;
        if (Items.Count > 0)
        {
            if (index < Items.Count
                && ContainerFromIndex(index) is { } container
                && container.TranslatePoint(new Point(), this) is { } point)
            {
                offset = horizontal ? point.X : point.Y;
            }
            else if (ContainerFromIndex(Items.Count - 1) is { } last
                     && last.TranslatePoint(new Point(), this) is { } lastPoint)
            {
                offset = horizontal ? lastPoint.X + last.Bounds.Width : lastPoint.Y + last.Bounds.Height;
            }
        }

        _insertionLine.Margin = horizontal
            ? new Thickness(offset, 0, 0, 0)
            : new Thickness(0, offset, 0, 0);
        _insertionLine.IsVisible = true;
    }

    private void ClearInsertion()
    {
        Classes.Remove(DragOverClass);
        if (_insertionLine is not null)
        {
            _insertionLine.IsVisible = false;
        }
    }
}
