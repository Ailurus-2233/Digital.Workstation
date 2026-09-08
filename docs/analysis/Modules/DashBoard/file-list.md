# DashBoard — 文件结构与功能

相对 `Modules/DashBoard/` 的目录树（`obj/`、`Output/` 为构建产物，略）：

```
DashBoard.csproj                       项目文件：net10.0，引用 Abstractions/Framework/Resource/UIPackage
DashBoardModule.cs                     Prism 模块入口：DashBoardModule（注册贡献与视图）
DashBoardMenus.cs                      文件菜单"打开启动台"项贡献：[MenuGroup]/[MenuItem] attribute 菜单类 DashBoardMenus
DashBoardOverviewMainView.cs           MainContent 概览主视图贡献：DashBoardOverviewMainView
DashBoardRecentMainView.cs             MainContent 最近项目主视图贡献：DashBoardRecentMainView
DashBoardTasksPanelTab.cs              BottomPanel"任务"面板 tab 贡献：DashBoardTasksPanelTab
DashBoardStatusBarItem.cs              状态栏条目贡献：DashBoardStatusBarItem
ViewModels/
  Windows/
    DashBoardWindowViewModel.cs        启动台进度窗 ViewModel：DashBoardWindowViewModel
Views/
  DashBoardNavigationView.axaml(.cs)   SideBar 内容视图：DashBoardNavigationView（含按钮点击→OpenMainViewEvent）
  DashBoardOverviewView.axaml(.cs)     概览主视图：DashBoardOverviewView（静态占位）
  DashBoardRecentView.axaml(.cs)       最近项目主视图：DashBoardRecentView（静态占位）
  DashBoardTasksView.axaml(.cs)        任务面板内容视图：DashBoardTasksView（静态占位）
  Windows/
    DashBoardWindow.axaml(.cs)         启动台进度窗：DashBoardWindow（Window）
```

## 逐文件说明

### DashBoard.csproj

`Microsoft.NET.Sdk`，`net10.0` + `ImplicitUsings` + `Nullable`（第 4-7 行）。四条 ProjectReference：Abstractions、Framework、Resource、UIPackage（第 20-23 行）。第 9-17 行两条 `Compile Update` 设置 `DependentUpon`（IDE 中 .axaml.cs 嵌套于 .axaml 下），其中 `DashBoardWindow.axaml.cs` 另带 `<SubType>Code</SubType>`——只有这两个文件有此条目，其余三个视图代码后置文件未配置嵌套。

### DashBoardModule.cs

`public class DashBoardModule : IModule`（第 7 行）。`RegisterTypes`（第 9 行）注册 5 个贡献单例（`DashBoardNavigationItem`/`DashBoardOverviewMainView`/`DashBoardRecentMainView`/`DashBoardTasksPanelTab`/`DashBoardStatusBarItem`）、经 `RegisterMenus(typeof(DashBoardModule).Assembly)`（第 15 行，`DigitalWorkstation.Core.Framework.Menus` 扩展，using 在第 2 行）反射注册 attribute 菜单类 `DashBoardMenus`，以及 4 个视图瞬态（`DashBoardNavigationView`/`DashBoardOverviewView`/`DashBoardRecentView`/`DashBoardTasksView`）。`OnInitialized`（第 23 行）空实现，注释说明启动台窗口由 shell 启动序列在模块加载前显示（ADR-0004）。

### DashBoardNavigationItem.cs

`public class DashBoardNavigationItem : INavigationItemContribution`（第 11 行），类注释自称"tracer bullet：验证模块到 shell 的贡献通路"。属性：`Id="dashboard"`、`Title=Language.DashBoardNavigationTitle`、`IconPath=Icons.DashBoard`、`Order=0`、`Placement=NavigationItemPlacement.Top`、`ContentViewType=typeof(DashBoardNavigationView)`。

### DashBoardOverviewMainView.cs / DashBoardRecentMainView.cs

两个 `IMainViewContribution` 实现（第 9 行），各含 `public const string ViewId`（`"dashboard.overview"` / `"dashboard.recent"`，第 11 行），`Id => ViewId`，`ViewType` 分别指向 `DashBoardOverviewView`/`DashBoardRecentView`。类注释分别说明演示"SideBar 条目 → MainContent 通路"与"单视图整体替换"。`ViewId` 常量被 `DashBoardNavigationView` 的按钮处理器引用。

### DashBoardTasksPanelTab.cs

`public class DashBoardTasksPanelTab : IPanelTabContribution`（第 11 行）。`Id="dashboard.tasks"`、`Title=Language.DashBoardTasksTabTitle`、`IconPath=Icons.Tasks`、`Order=15`（行内注释：介于 shell 预置"输出"(10) 与"日志"(20) 之间，验证按 Order 排序）、`Panel=PanelPlacement.Bottom`、`ContentViewType=typeof(DashBoardTasksView)`。

### DashBoardStatusBarItem.cs

`public class DashBoardStatusBarItem : IStatusBarItemContribution`（第 11 行）。`Id="dashboard.status"`、`Title=Language.DashBoardNavigationTitle`（**复用导航项标题资源**）、`IconPath=Icons.DashBoard`、`Order=20`（类注释：排在 shell 预置"就绪"(10) 之后）。无视图——状态栏项只显示标题与图标。

### DashBoardMenus.cs

attribute 菜单类（ADR-0001 路径/分组模型），**不实现 `IMenuItemContribution`**。类级 `[MenuGroup("MenuFileTitle", Group = "General", GroupOrder = 100)]`（第 11 行）：单段路径，`MenuFileTitle` 为顶层"文件"菜单的 Language 资源键，`General` 组 `GroupOrder=100` 排在 shell 预置"退出"所属 Application 组（1000）之前（类注释第 8-10 行说明此意图）。主构造 `DashBoardMenus(IWindowManager windowManager)`（第 12 行）注入窗口管理器。唯一菜单方法 `[MenuItem("DashBoardOpenWindowMenuTitle", Order = 100, Icon = Icons.DashBoard)] public void OpenDashBoard()`（第 17-21 行）：标题为 Language 资源键（注册时经 `Language.Get` 解析），`Order=100` 为 General 组内位次，方法体调 `windowManager.ShowWindow<DashBoardWindow>()`。由 `DashBoardModule.RegisterTypes` 的 `RegisterMenus` 扫描注册——`DashBoardMenus` 本身注册为单例，方法被包装成一个 `IMenuItemContribution` 工厂。

### ViewModels/Windows/DashBoardWindowViewModel.cs

`public partial class DashBoardWindowViewModel : ObservableObject`（第 12 行），启动台进度窗 ViewModel。成员清单（完整）：

| 成员 | 种类 | 说明 |
|---|---|---|
| `_eventAggregator` | 字段（第 14 行，`readonly`） | 构造注入的事件聚合器 |
| `DashBoardWindowViewModel(IEventAggregator)` | 构造函数（第 16 行） | 订阅 `StartupProgressEvent`→`OnProgress`、`ModuleLoadFailedEvent`→`OnModuleFailed`（均 `ThreadOption.UIThread, true`） |
| `_phaseText` → `PhaseText` | `[ObservableProperty]`（第 23-24 行） | 阶段文案，初值 `Language.SplashStartingText` |
| `_moduleText` → `ModuleText` | `[ObservableProperty]`（第 29-30 行） | 模块名 + i/N，非加载模块阶段为空 |
| `_isFailed` → `IsFailed` | `[ObservableProperty]`（第 32-33 行） | 失败态开关 |
| `_errorMessage` → `ErrorMessage` | `[ObservableProperty]`（第 35-36 行） | 失败错误详情 |
| `OnProgress(StartupProgress)` | 私有方法（第 38 行） | 进度事件回调：复位 `IsFailed`、switch 映射 `PhaseText`、按阶段设置/清空 `ModuleText` |
| `OnModuleFailed(ModuleLoadFailure)` | 私有方法（第 53 行） | 失败事件回调：置 `IsFailed`、失败文案、`ModuleText`、`ErrorMessage` |
| `FormatModuleText(string?, int, int)` | 私有静态方法（第 61 行） | `$"{moduleName}（{index}/{count}）"`（全角括号） |
| `Continue()` → `ContinueCommand` | `[RelayCommand]`（第 69-70 行） | 发布 `StartupFailureActionEvent(StartupFailureAction.Continue)` |
| `Exit()` → `ExitCommand` | `[RelayCommand]`（第 78-80 行） | 发布 `StartupFailureActionEvent(StartupFailureAction.Exit)` |

> 注：本类**不存在**名为 `SetProgress` 的成员（上游 Core/Framework 文档调查期间曾出现该名）；进度回调方法名是 `OnProgress`。

### Views/Windows/DashBoardWindow.axaml(.cs)

启动台窗口。axaml 第 1-13 行：`Window`，`CanResize="False"`、`SizeToContent="Height"`、`Width="440"`、`WindowStartupLocation="CenterScreen"`、`Title="启动台"`（硬编码中文，未走 Language）、`prism:ViewModelLocator.AutoWireViewModel="True"`。内容（第 14-32 行，`StackPanel Margin="24" Spacing="12"`）：
- 标题 `TextBlock`"Digital.Workstation"（FontSize 18 SemiBold，硬编码）；
- `TextBlock Text="{Binding PhaseText}"`（FontSize 14）；
- `ProgressBar Height="4" IsIndeterminate="{Binding !IsFailed}"`（失败时停止滚动）；
- `TextBlock Text="{Binding ModuleText}"`（`Foreground=SemiColorText2`）；
- 错误区 `Border IsVisible="{Binding IsFailed}"`（`Background=SemiColorFill0`、`CornerRadius=4`、`Padding=12`）：`TextBlock Text="{Binding ErrorMessage}"`（`MaxWidth=392`、`SemiColorDanger`、自动换行）+ 右对齐按钮行"继续"（`ContinueCommand`）/"退出"（`ExitCommand`）。

代码后置（.axaml.cs）：`public partial class DashBoardWindow : Window`，仅无参构造 `InitializeComponent()`。

### Views/DashBoardNavigationView.axaml(.cs)

SideBar 内容视图。axaml 定义局部样式 `Button.sidebar-entry`（第 11-28 行：透明背景、无框、圆角 4、`Padding=12,8`、左对齐、pointerover 时 `SemiColorFill1`、内嵌 TextBlock 前景 `SemiColorText0`）与两个按钮（第 31-36 行）："概览"（`Click="OpenOverview"`）、"最近项目"（`Click="OpenRecent"`，按钮文本均硬编码中文）。

代码后置：`public partial class DashBoardNavigationView : UserControl`（第 11 行）。双构造（第 18-26 行）：无参转发 `IoC.Provider.Resolve<IEventAggregator>()`（注释：XAML runtime loader 需要无参构造；实际实例由容器经依赖注入构造创建）；注入构造保存 `_eventAggregator` 并 `InitializeComponent()`。处理器 `OpenOverview`（第 28 行）/ `OpenRecent`（第 33 行）分别 `Publish OpenMainViewEvent(DashBoardOverviewMainView.ViewId / DashBoardRecentMainView.ViewId)`。

### Views/DashBoardOverviewView.axaml(.cs) / DashBoardRecentView.axaml(.cs) / DashBoardTasksView.axaml(.cs)

三个静态占位 `UserControl`，无 ViewModel、无逻辑。Overview/Recent 为居中标题 + 说明文本（"概览：DashBoard 概览视图（演示 MainContent 单视图切换）"；"最近项目：最近项目视图（演示整体替换，无文档 tabs）"）；Tasks 为单条 `TextBlock`"DashBoard 任务面板（演示模块贡献 BottomPanel tab）"（`Margin=12`、`SemiColorText2`）。代码后置均只有无参构造 `InitializeComponent()`。三者是 shell 按 `ContentViewType`/`ViewType` 经容器解析的目标。
