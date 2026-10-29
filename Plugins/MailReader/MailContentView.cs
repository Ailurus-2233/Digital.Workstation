using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using DigitalWorkstation.MailReader.Resources;

namespace DigitalWorkstation.MailReader;

// Renders only the parsed data model. HTML, CSS, image paths and XAML are never executed.
public sealed class MailContentView : StackPanel
{
    public static readonly StyledProperty<MailMessage?> MessageProperty =
        AvaloniaProperty.Register<MailContentView, MailMessage?>(nameof(Message));
    private readonly List<Bitmap> _bitmaps = [];
    public MailMessage? Message { get => GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
    public MailContentView() { Spacing = 10; }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MessageProperty) Render();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e); Clear();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e); Render();
    }
    private void Clear()
    {
        Children.Clear();
        foreach (var bitmap in _bitmaps) bitmap.Dispose();
        _bitmaps.Clear();
    }
    private static SelectableTextBlock Text(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };
    private void Render()
    {
        Clear();
        var message = Message;
        if (message is null) return;
        if (message.Content is not { } content) { Children.Add(Text(message.Body)); return; }
        foreach (var block in content.Blocks)
        {
            if (block.Kind == "unsupported-image")
            {
                Children.Add(Text($"[{MailResources.InlineImageUnavailable}] {block.Description}"));
                continue;
            }
            if (block.Kind == "image") { RenderImage(block, content); continue; }
            var text = Text("");
            text.Margin = new Thickness(Math.Min(block.Level, 12) * 16, 0, 0, 0);
            text.FontSize = block.Kind == "heading" ? Math.Clamp(26 - block.HeadingLevel * 2, 16, 24) : 14;
            text.FontWeight = block.Kind == "heading" ? FontWeight.SemiBold : FontWeight.Normal;
            if (block.Kind == "quote") text.FontStyle = FontStyle.Italic;
            foreach (var run in block.Runs)
                text.Inlines!.Add(new Run(run.Text)
                {
                    FontWeight = run.Bold ? FontWeight.Bold : text.FontWeight,
                    FontStyle = run.Italic ? FontStyle.Italic : text.FontStyle,
                    TextDecorations = run.Link is not null ? TextDecorations.Underline : null
                });
            Children.Add(text);
            // Keep all paragraph text selectable; explicit links below show their actual destination.
            foreach (var link in block.Runs.Select(r => r.Link).OfType<string>().Distinct())
            {
                if (!MailHtmlReader.TryWebLink(link, out var uri)) continue;
                var button = new Button { Content = Text(link), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
                ToolTip.SetTip(button, link);
                button.Click += async (_, _) =>
                {
                    try
                    {
                        if (TopLevel.GetTopLevel(this) is not { } top || !await top.Launcher.LaunchUriAsync(uri!))
                            Children.Add(Text(MailResources.LinkFailed));
                    }
                    catch { Children.Add(Text(MailResources.LinkFailed)); }
                };
                Children.Add(button);
            }
        }
        if (content.Attachments.Length == 0) return;
        Children.Add(new TextBlock { Text = MailResources.Attachments, FontWeight = FontWeight.SemiBold });
        foreach (var attachment in content.Attachments)
        {
            var status = Text("");
            var label = $"{attachment.Name} ({attachment.Size?.ToString() ?? "?"} bytes)";
            var button = new Button { Content = Text($"{MailResources.SaveAttachment}: {label}") };
            button.Click += async (_, _) =>
            {
                button.IsEnabled = false;
                try
                {
                    var saved = await MailAttachmentSaver.SaveAsync(attachment, async () =>
                    {
                        var top = TopLevel.GetTopLevel(this);
                        if (top?.StorageProvider.CanSave != true) throw new NotSupportedException();
                        using var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                        { Title = MailResources.SaveAttachment, SuggestedFileName = attachment.Name, ShowOverwritePrompt = true });
                        if (file is null) return null;
                        return file.TryGetLocalPath() ?? throw new NotSupportedException();
                    });
                    status.Text = saved ? MailResources.AttachmentSaved : MailResources.SaveCancelled;
                }
                catch { status.Text = MailResources.AttachmentSaveFailed; }
                finally { button.IsEnabled = true; }
            };
            Children.Add(button); Children.Add(status);
        }
    }
    private void RenderImage(MailBlock block, MailContent content)
    {
        var matches = content.Images.Where(i => i.Id == block.ImageId).ToArray();
        if (block.ImageId is not null && matches.Length == 1)
        {
            var image = matches[0];
            // SVG and other active/vector formats are never passed to a rendering engine.
            if (image.MediaType.ToLowerInvariant() is "image/png" or "image/jpeg" or "image/gif" or "image/bmp" or "image/webp")
            {
                try
                {
                    var bitmap = MailImageDecoder.Decode(image.Data);
                    _bitmaps.Add(bitmap);
                    Children.Add(new Image { Source = bitmap, MaxWidth = 900, MaxHeight = 600, Stretch = Stretch.Uniform,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left });
                    if (!string.IsNullOrWhiteSpace(block.Description)) Children.Add(Text(block.Description));
                    return;
                }
                catch { /* A broken or unsupported image remains visible as an explanation below. */ }
            }
        }
        Children.Add(Text($"[{(block.ImageId is null ? MailResources.RemoteImageBlocked : MailResources.InlineImageUnavailable)}] {block.Description}"));
    }
}

