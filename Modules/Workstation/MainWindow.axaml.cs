using Avalonia.Controls;

namespace DigitalWorkstation.Workstation;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        // 模块贡献在 Prism 模块初始化（晚于 shell 创建）时才注册，首次显示时再收集
        (DataContext as MainWindowViewModel)?.EnsureContributionsLoaded();
    }
}
