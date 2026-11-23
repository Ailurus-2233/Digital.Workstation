using Avalonia;
using Avalonia.Controls;

namespace DigitalWorkstation.Settings.Views;

public sealed class DraftLabel : TextBlock
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<DraftLabel, string?>(nameof(Label));
    public static readonly StyledProperty<bool> HasDraftProperty =
        AvaloniaProperty.Register<DraftLabel, bool>(nameof(HasDraft));

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public bool HasDraft
    {
        get => GetValue(HasDraftProperty);
        set => SetValue(HasDraftProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(TextBlock);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LabelProperty || change.Property == HasDraftProperty)
            Text = HasDraft ? $"{Label} ·" : Label;
    }
}
