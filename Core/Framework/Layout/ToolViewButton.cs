using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace DigitalWorkstation.Core.Framework.Layout;

/// <summary>
///     可拖拽的工具视图按钮（面板 tab 头 / ActivityBar 导航项，ADR-0002）：左键按下并移动
///     超过阈值后以 DoDragDrop 发起拖拽，负载为 <see cref="DragTabId" />；<see cref="CanDrag" />
///     为 false（钉住项）时不发起拖拽，保持普通点击。控件不感知 ViewModel 类型，
///     DragTabId/CanDrag 在主题模板里宽松绑定
/// </summary>
public class ToolViewButton : Button
{
    /// <summary>
    ///     触发拖拽的最小位移（逻辑像素）
    /// </summary>
    private const double DragThreshold = 4;

    public static readonly StyledProperty<string?> DragTabIdProperty =
        AvaloniaProperty.Register<ToolViewButton, string?>(nameof(DragTabId));

    public static readonly StyledProperty<bool> CanDragProperty =
        AvaloniaProperty.Register<ToolViewButton, bool>(nameof(CanDrag), true);

    private Point? _dragStart;

    /// <summary>
    ///     ControlTheme 按 StyleKey 精确查找：继承 Button 的全部主题（含 nav-item/panel-tab 类样式）
    /// </summary>
    protected override Type StyleKeyOverride => typeof(Button);

    /// <summary>
    ///     拖拽负载：工具视图 Id
    /// </summary>
    public string? DragTabId
    {
        get => GetValue(DragTabIdProperty);
        set => SetValue(DragTabIdProperty, value);
    }

    /// <summary>
    ///     是否允许发起拖拽；钉住项（AllowMove=false）绑定为 false
    /// </summary>
    public bool CanDrag
    {
        get => GetValue(CanDragProperty);
        set => SetValue(CanDragProperty, value);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (CanDrag && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _dragStart = e.GetPosition(this);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _dragStart = null;
        base.OnPointerReleased(e);
    }

    protected override async void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragStart is not { } start || DragTabId is not { } tabId)
        {
            return;
        }

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _dragStart = null;
            return;
        }

        var current = e.GetPosition(this);
        if (Math.Abs(current.X - start.X) < DragThreshold && Math.Abs(current.Y - start.Y) < DragThreshold)
        {
            return;
        }
        // 拖拽一旦开始，指针捕获移交给 DnD 会话，本按钮不再收到 Release，Click 不会触发
        _dragStart = null;
        var data = new DataTransfer();
        data.Add(DataTransferItem.Create(ToolViewDragSession.TabIdFormat, tabId));
        ToolViewDragSession.Begin();
        try
        {
            await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move);
        }
        finally
        {
            ToolViewDragSession.End();
        }
    }
}
