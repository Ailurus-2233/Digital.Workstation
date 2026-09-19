# Workstation — 文件结构与功能

相对 `Modules/Workstation/` 的目录树（不含 `obj/`、`Output/` 构建产物）：

```
Workstation.csproj                  项目文件：net10.0；引用 Framework/Resource/UIPackage/DashBoard/Settings；2 条 DependentUpon
WorkstationApplication.cs           应用入口：WorkstationApplication : FrameworkApplication<MainWindow>
MainWindow.axaml                    主窗口 XAML（41 行）：FrameworkWindow，只留应用级 chrome（窗口标题；快捷键已迁移为命令 Gesture）
MainWindow.axaml.cs                 主窗口 code-behind：PrepareContributions 在 Ready 前收集呈现、注册原生菜单和命令手势
MainWindowViewModel.cs              主窗口 ViewModel（703 行）：布局状态机驱动 + 面板对齐档位 + 贡献收集（含命令 Commands，[ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)）+ 统一视图缓存（[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）+ 布局持久化接线 + 拖拽落放（MoveTabCommand）+ ActivityBar"设置"导航按钮（OpenSettingsCommand/SettingsIcon/SettingsTitle，[ADR-0006](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 6）
NavigationItemViewModel.cs          ActivityBar 导航项呈现模型（ObservableObject，IsSelected）
PanelTabViewModel.cs                面板 tab 呈现模型（ObservableObject，IsActive）
StatusBarItemViewModel.cs           状态栏条目呈现模型（普通类）
Contributions/
  ReadyStatusBarItem.cs             状态栏"就绪"贡献（Order 10；Contributions/ 唯一贡献类——工具视图已改 [ToolView] attribute，[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）
Menus/
  FileMenus.cs                      文件菜单类（[MenuGroup("shell.file", typeof(WorkstationResources), nameof(WorkstationResources.MenuFileTitle), Group="Application", GroupOrder=1000, Order=100)]，Exit 方法 Shutdown()）
  FileNavigationMenus.cs           文件菜单 Navigation 组及命令：回到主页（无快捷键）与首选项（Ctrl+,）；分别发布 ReturnHomeEvent 与 OpenMainViewEvent
  ViewPanelMenus.cs                 视图菜单 Panels 组类（三个显隐切换方法发布 TogglePanelVisibilityEvent）
  ViewAlignmentMenus.cs             视图菜单 Alignment 组类（四个对齐方法发布 SetPanelAlignmentEvent）
  ViewLayoutMenus.cs                 视图菜单 Layout 组类（GroupOrder=300，单项"重置布局"发布 ResetLayoutEvent）
  HelpMenus.cs                      帮助菜单类（[MenuGroup("shell.help", typeof(WorkstationResources), nameof(WorkstationResources.MenuHelpTitle), Order=300)]，About 方法 ShowDialog<AboutWindow>）
Commands/
  ViewCommands.cs                   shell 预置命令类（[ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)）：四个 [Command] 方法（三面板显隐切换 + 重置布局，复用视图菜单标题键与事件通路；三面板命令带 Icons.PanelLeft/PanelBottom/PanelRight 图标（与对应菜单项一致）与 Gesture（Ctrl+B/Ctrl+J/Ctrl+Alt+B，即面板显隐快捷键））
ViewModels/
  EmptyStateViewModel.cs            主页加载快照与所选模块名称；注入 IModuleCatalog，Refresh 在挂载时读取实际加载状态
Views/
  EmptyStateView.axaml(.cs)         产品主页：无参构造、Prism 自动关联 ViewModel；模块树与说明区域绑定呈现，挂载和选择事件为视图接线
  AboutWindow.axaml(.cs)            "关于"对话框：360×160 不可调大小、CenterOwner，硬编码中文文案
```

## 逐文件说明
- **Workstation.csproj**：链接 `Launcher/Assets/AppIcon.png` 为 `Assets/AppIcon.png` Avalonia 资源，供主页 Image 使用；更换 SVG 图稿时需同步导出 PNG。
- **主页导航接线**：`MainWindowViewModel` 构造时缓存 `_homeContent` 并订阅 `ReturnHomeEvent`；`ReturnHome` 清除活动主视图、恢复缓存主页，不重置面板布局。`FileNavigationMenus` 同时被菜单与命令扫描发现，新增后共六个菜单类，命令宿主为 `ViewCommands` 与 `FileNavigationMenus`。
- **WorkstationApplication.cs**：不在构造期设置 `Application.Name`；产品名由 Framework 载入已存语言后统一赋值，避免 macOS 最左侧应用菜单提前固化为中性资源中文。其余成员负责模块目录、贡献注册与启动台创建。
- **MainWindow.axaml**：标题通过 `{x:Static resource:SharedResources.ProductName}` 本地化。窗口顶部为标题栏预留 32px；`u|TitleBar` 与 `TitleBarContent` 同高，标题文本垂直居中并下移 2px 做 macOS 光学校正。
- **MainWindow.axaml.cs**：构造加载 XAML；PrepareContributions 在 Ready 前初始化 VM、原生菜单和命令手势，不再订阅 Opened。
- **MainWindowViewModel.cs**：订阅工作区事件，缓存主页与工具/主视图实例；EnsureContributionsLoaded 初始化贡献呈现；LoadToolViews 委托 ShellLayoutConfiguration.Restore；全部布局动作经 ApplyLayout 同步状态、集合、高亮与宿主，按同一状态捕获保存；ResetLayout 保留主视图并恢复默认后删除配置。
- **三个呈现模型**（`NavigationItemViewModel`/`PanelTabViewModel`/`StatusBarItemViewModel`）：结构同构——构造接收贡献元数据、解析 `Icon` 几何、透传 `Id`/`Title`；前两者包装 `ToolViewContribution`（[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)），`IconPath` 为 null 时 `Icon` 为 null（各 :15）；后者包装 `IStatusBarItemContribution`，`IconPath` 必填（:14）（见 api.md 第 3 节）。`MenuItemViewModel` 已迁入 Framework（`Core/Framework/Menus/MenuItemViewModel.cs`），本模块经 `using DigitalWorkstation.Core.Framework.Menus` 解析。
- **Contributions/ + Menus/ + Commands/**：一个状态栏贡献、六个 attribute 菜单类、两个命令宿主（`ViewCommands` 与同时声明菜单项的 `FileNavigationMenus`）。菜单由 `[MenuGroup]` + `[MenuItem]` 声明，命令由方法上的 `[Command]` 声明，均自动扫描；具体映射见 api.md 第 5 节。当前无 `[ToolView]` 实例。
- **Views/**：`EmptyStateView` 与 `AboutWindow` 的产品名通过 `SharedResources.ProductName` 获取，关于窗口标题通过 `WorkstationResources.AboutWindowTitle` 获取；两者随当前 UI 语言切换。

## 私有资源文件

- `Resources/WorkstationResources.cs`：公开静态资源 facade。
- `Resources/WorkstationResources.resx`：中文中性资源。
- `Resources/WorkstationResources.en-US.resx`：英文卫星资源。

三者同位、同基名；SDK 默认嵌入资源，基名等于 `DigitalWorkstation.Workstation.Resources.WorkstationResources`。

WorkstationApplication.PrepareShell 是启动序列调用 MainWindow.PrepareContributions 的接线点；状态栏手写贡献改用 RegisterShellContribution，以接受启动准备和失败隔离。
