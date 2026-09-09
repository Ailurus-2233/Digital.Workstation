﻿using Avalonia.Controls;
using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.UIPackage;

namespace DigitalWorkstation.Workstation.Views;

/// <summary>
///     shell 预置的 AuxiliaryPanel 演示工具视图"大纲"
/// </summary>
[ToolView("shell.outline", "OutlineTabTitle", Icon = Icons.Outline, Order = 20)]
public partial class OutlineView : UserControl
{
    public OutlineView()
    {
        InitializeComponent();
    }
}
