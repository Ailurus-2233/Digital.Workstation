using DigitalWorkstation.Core.Framework.Windows;

namespace DigitalWorkstation.Workstation;

public partial class MainWindow : FrameworkWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    internal void PrepareContributions()
    {
        // 模块贡献准备成功后，在 Ready 和显示主窗口之前收集呈现。
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }
        viewModel.EnsureContributionsLoaded();
        RegisterNativeMenu(viewModel.MenuBarItems);
        // 命令手势 KeyBinding（ADR-0005 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)）：机制在 Framework（RegisterCommandGestures），接线在本模块
        RegisterCommandGestures(viewModel.Commands);
    }
}
