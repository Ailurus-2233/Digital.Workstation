using Avalonia.Controls;

using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.Framework.Layout;

namespace DigitalWorkstation.Workstation;

public sealed class MainTab(string viewId, string? documentId, string title, object content)
{
    public string ViewId { get; } = viewId;
    public string? DocumentId { get; } = documentId;
    public string Title { get; } = title;
    public object Content { get; } = content;
    public IEditableTab? Editor => Content is Control page ? TabEditing.GetEditor(page) : null;
    public ContextMenu ContextMenu { get; } = new();
}
