# DashBoard — 文件结构与功能

相对 `Modules/DashBoard/` 的目录树（`obj/`、`Output/` 为构建产物，略）：

```
DashBoard.csproj                       项目文件：net10.0，引用 Abstractions/Framework/Resource/UIPackage
DashBoardModule.cs                     Prism 模块入口：DashBoardModule（注册状态栏贡献）
DashBoardStatusBarItem.cs              状态栏条目贡献：DashBoardStatusBarItem
ViewModels/
  Windows/
    DashBoardWindowViewModel.cs        启动台进度窗 ViewModel：DashBoardWindowViewModel
Views/
  Windows/
    DashBoardWindow.axaml(.cs)         启动台进度窗：DashBoardWindow（Window）
```

## 逐文件说明

### DashBoard.csproj

`Microsoft.NET.Sdk`，`net10.0` + `ImplicitUsings` + `Nullable`（第 4-7 行）。四条 ProjectReference：Abstractions、Framework、Resource、UIPackage（第 17-20 行）。第 9-13 行一条 `Compile Update` 设置 `DependentUpon`（IDE 中 `DashBoardWindow.axaml.cs` 嵌套于 `DashBoardWindow.axaml` 下，另带 `<SubType>Code</SubType>`）。

### DashBoardModule.cs

`public class DashBoardModule : IModule`（第 6 行）。`RegisterTypes`（第 8 行）：`RegisterToolViews(typeof(DashBoardModule).Assembly)`（第 12 行，`DigitalWorkstation.Core.Framework.Contributions` 扩展，using 在第 2 行）扫描本程序集 `[ToolView]` 类——当前无标注类（原 `DashBoardNavigationView`/`DashBoardTasksView` 已删除），注册为空，保留以覆盖将来新增（ADR-0002）；再注册 1 个 `IStatusBarItemContribution` 单例（第 13 行）。`OnInitialized`（第 16 行）空实现，注释说明启动台窗口由 shell 启动序列在模块加载前显示（ADR-0004）。


### DashBoardStatusBarItem.cs

`public class DashBoardStatusBarItem : IStatusBarItemContribution`（第 11 行）。`Id="dashboard.status"`、`Title=Language.DashBoardNavigationTitle`（启动台标题资源）、`IconPath=Icons.DashBoard`、`Order=20`（类注释：排在 shell 预置"就绪"(10) 之后）。无视图——状态栏项只显示标题与图标。

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

