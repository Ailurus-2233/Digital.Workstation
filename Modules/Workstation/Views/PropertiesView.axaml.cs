﻿using Avalonia.Controls;
using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.UIPackage;

namespace DigitalWorkstation.Workstation.Views;

/// <summary>
///     shell 预置的 AuxiliaryPanel 演示工具视图"属性"
/// </summary>
[ToolView("shell.properties", "PropertiesTabTitle", Icon = Icons.Properties, Order = 10)]
public partial class PropertiesView : UserControl
{
    public PropertiesView()
    {
        InitializeComponent();
    }
}
