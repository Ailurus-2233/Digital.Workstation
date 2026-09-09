﻿using Avalonia.Controls;
using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.UIPackage;

namespace DigitalWorkstation.DashBoard.Views;

/// <summary>
///     DashBoard 的 BottomPanel 演示工具视图"任务"（验证模块到面板的贡献通路）
/// </summary>
[ToolView("dashboard.tasks", "DashBoardTasksTabTitle", Icon = Icons.Tasks,
    Default = ToolViewPlacement.BottomPanel, Order = 15)]
public partial class DashBoardTasksView : UserControl
{
    public DashBoardTasksView()
    {
        InitializeComponent();
    }
}
