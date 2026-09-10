using DigitalWorkstation.Core.Framework.Windows;

namespace DigitalWorkstation.Workstation;

public partial class MainWindow : FrameworkWindow
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        // 模块贡献在 Prism 模块初始化（晚于 shell 创建）时才注册，首次显示时再收集
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }
        viewModel.EnsureContributionsLoaded();
        // 命令手势 KeyBinding（ADR-0005）：机制在 Framework（RegisterCommandGestures），接线在本模块
        RegisterCommandGestures(viewModel.Commands);
    }
}
