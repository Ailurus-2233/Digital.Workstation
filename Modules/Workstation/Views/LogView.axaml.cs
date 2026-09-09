﻿using Avalonia.Controls;
using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.UIPackage;

namespace DigitalWorkstation.Workstation.Views;

/// <summary>
///     shell 预置的 BottomPanel 演示工具视图"日志"
/// </summary>
[ToolView("shell.log", "LogTabTitle", Icon = Icons.Log,
    Default = ToolViewPlacement.BottomPanel, Order = 20)]
public partial class LogView : UserControl
{
    public LogView()
    {
        InitializeComponent();
    }
}
