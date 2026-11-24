using Avalonia;
using Avalonia.Controls;
using DigitalWorkstation.Core.Abstractions.Contributions;

namespace DigitalWorkstation.Core.Framework.Layout;

/// <summary>在页面根控件上绑定或设置处理草稿的对象。</summary>
public sealed class TabEditing : AvaloniaObject
{
    public static readonly AttachedProperty<IEditableTab?> EditorProperty =
        AvaloniaProperty.RegisterAttached<TabEditing, Control, IEditableTab?>("Editor");

    public static IEditableTab? GetEditor(Control page) => page.GetValue(EditorProperty);
    public static void SetEditor(Control page, IEditableTab? value) => page.SetValue(EditorProperty, value);
}
