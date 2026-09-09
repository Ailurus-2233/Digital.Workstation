﻿using Avalonia.Controls;
using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.UIPackage;

namespace DigitalWorkstation.Workstation.Views;

/// <summary>
///     shell 预置的 BottomPanel 演示工具视图"输出"
/// </summary>
[ToolView("shell.output", "OutputTabTitle", Icon = Icons.Output,
    Default = ToolViewPlacement.BottomPanel, Order = 10)]
public partial class OutputView : UserControl
{
    public OutputView()
    {
        InitializeComponent();
    }
}
