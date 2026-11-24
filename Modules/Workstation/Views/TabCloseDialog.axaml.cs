using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DigitalWorkstation.Workstation.Views;

public enum TabCloseChoice { Cancel, Save, Discard }

public partial class TabCloseDialog : Window
{
    public TabCloseDialog() => InitializeComponent();

    public static async Task<TabCloseChoice> ShowAsync(Window owner, string title, string message)
    {
        var dialog = new TabCloseDialog { Title = title };
        dialog.Message.Text = message;
        return await dialog.ShowDialog<TabCloseChoice?>(owner) ?? TabCloseChoice.Cancel;
    }

    private void Save(object? sender, RoutedEventArgs e) => Close(TabCloseChoice.Save);
    private void Discard(object? sender, RoutedEventArgs e) => Close(TabCloseChoice.Discard);
    private void Cancel(object? sender, RoutedEventArgs e) => Close(TabCloseChoice.Cancel);
}
