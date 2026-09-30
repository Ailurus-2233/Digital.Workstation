# Framework — 文件清单

Framework 保留应用引导、Shell 状态机与窗口模板、贡献扫描收集、设置声明和 UI 资源。基础服务文件已迁至 [Services/file-list.md](../Services/file-list.md)，PanelAlignment 位于 Abstractions/Layout，SetPanelAlignmentEvent 位于 Common/Events。

- Commands/CommandRegistration.cs
- Contributions/ShellContributionCollector.cs
- Contributions/ToolViewRegistration.cs
- Framework.csproj
- FrameworkApplication.cs
- Layout/AuxiliaryPanelState.cs
- Layout/BottomPanelState.cs
- Layout/MainContentState.cs
- Layout/PanelResize.cs
- Layout/PanelResizer.cs
- Layout/PanelResizeTarget.cs
- Layout/ShellLayoutConfiguration.cs
- Layout/ShellLayoutMetrics.cs
- Layout/ShellLayoutState.cs
- Layout/SideBarState.cs
- Layout/ToolViewBar.cs
- Layout/ToolViewButton.cs
- Layout/ToolViewDragSession.cs
- Layout/ToolViewMove.cs
- Menus/MenuItemViewModel.cs
- Menus/MenuRegistration.cs
- Menus/MenuTreeBuilder.cs
- Menus/MenuTreeEntry.cs
- Resources/FrameworkResources.cs
- Resources/FrameworkResources.en-US.resx
- Resources/FrameworkResources.resx
- Settings/GeneralSettings.cs
- Settings/SettingRegistration.cs
- Settings/UiLanguage.cs
- Windows/CommandPalette.cs
- Windows/FrameworkWindow.cs
- Windows/FrameworkWindowTheme.axaml
- Windows/FrameworkWindowTheme.cs

模块服务接线在 FrameworkApplication.cs：加载完成后、贡献 Prepare 前 Start；失败 Stop 当前批次；真实 Exit 先 Dispose 模块服务目录再 Dispose 基础服务。实现文件归 Core/Services/Lifecycle。
