# DashBoard — 模块关系链

## 依赖关系

### 项目引用（DashBoard.csproj:17-20）

| 依赖 | 用到的能力 | 本模块使用点 |
|---|---|---|
| `Core/Abstractions` | Shell 贡献契约：`IStatusBarItemContribution`（Abstractions/Contributions/）；工具视图 attribute `ToolViewAttribute` 与放置枚举 `ToolViewPlacement`（ADR-0002）仅经 `RegisterToolViews` 扫描机制涉及，当前无标注类 | `DashBoardStatusBarItem.cs:1、11` 实现接口（using `DigitalWorkstation.Core.Abstractions.Contributions`，`DashBoardModule.cs:1`） |
| `Core/Framework` | 间接获得 Prism（`IModule`、`IContainerRegistry`、`IEventAggregator`、`PubSubEvent`、`ThreadOption`）与 CommunityToolkit.Mvvm（`ObservableObject`、`[ObservableProperty]`、`[RelayCommand]`）的传递引用；运行期由其提供 `IoC` 初始化与 `RegisterToolViews` 工具视图注册扩展（Framework/Contributions/ToolViewRegistration.cs，ADR-0002） | `DashBoardModule.cs:2、6、12` `IModule` 与 `RegisterToolViews`（using `DigitalWorkstation.Core.Framework.Contributions`，第 2 行）；`DashBoardWindowViewModel.cs:1-2、12、69、78` |
| `Core/Resource` | `Language` 本地化字符串：`DashBoardNavigationTitle`、`SplashStartingText`、`SplashPhaseCoreServices`、`SplashPhaseLoadingModules`、`SplashPhaseReady`、`SplashPhaseFailed`（Resource/Language.cs；中英值在 Language.resx / Language.en-US.resx） | `DashBoardStatusBarItem.Title` 属性（DashBoardStatusBarItem.cs:15）；`DashBoardWindowViewModel.cs:24、43-45、56` |
| `Core/UIPackage` | `Icons` 图标路径常量：`Icons.DashBoard`（四宫格）（UIPackage/Icons.cs:18） | `DashBoardStatusBarItem.cs:17` |

### 传递依赖（未在 csproj 直接引用，但源码 using 其命名空间）

| 传递来源 | 用到的能力 | 使用点 |
|---|---|---|
| `Core/Models`（经 Framework 传递） | 启动事件三件套：`StartupProgressEvent`/`StartupProgress`/`StartupPhase`、`ModuleLoadFailedEvent`/`ModuleLoadFailure`、`StartupFailureActionEvent`/`StartupFailureAction`（均 Models/Events/） | `DashBoardWindowViewModel.cs:3、19-20、38、53、72、81` |

### NuGet / 框架

`net10.0`、`ImplicitUsings`+`Nullable` enable（DashBoard.csproj:4-7）。Avalonia（`Window`/`UserControl`）、Prism、CommunityToolkit.Mvvm 均经 Framework 传递。csproj 第 9-13 行一条 `Compile Update ... DependentUpon`（IDE 嵌套显示，对构建无语义影响）：`Views\Windows\DashBoardWindow.axaml.cs`（带 `SubType=Code`）。

## 被依赖关系

| 依赖方 | 引用方式 | 用在什么场景 |
|---|---|---|
| `Modules/Workstation`（Workstation.csproj） | ProjectReference | 应用宿主：`WorkstationApplication.cs:18` `moduleCatalog.AddModule<DashBoardModule>()` 注册模块；`WorkstationApplication.cs:41-44` `CreateSplashWindow()` 重写返回 `Container.Resolve<DashBoardWindow>()`——启动序列逐模块加载前直接解析显示启动台（ADR-0004） |
| `Digital.Workstation.slnx`（第 11 行） | 解决方案成员 | `/Modules/` 文件夹下两个项目之一（另一个是 Workstation） |

除 Workstation 宿主外**无任何项目引用 DashBoard**；shell（Workstation 内）对贡献的消费全部经 Abstractions 接口完成，不引用本模块程序集。`UnitTest/` 下无 DashBoard 测试项目（见 testing.md）。

## 核心内部数据结构

### 贡献声明矩阵（声明值详表见 api.md）

唯一的接口贡献类无字段、无状态，全部数据即属性值：

```
DashBoardStatusBarItem.Id   "dashboard.status"
```

### `DashBoardWindowViewModel` 内部状态（ViewModels/Windows/DashBoardWindowViewModel.cs）

- `private readonly IEventAggregator _eventAggregator`（第 14 行）：构造注入，存活期与 ViewModel 相同；订阅用 `keepSubscriberReferenceAlive: true`，事件聚合器持强引用，无需显式退订。
- 四个 `[ObservableProperty]` 字段（第 23-36 行）即全部 UI 状态：`PhaseText`/`ModuleText`/`IsFailed`/`ErrorMessage`；状态机只有两个有效组合——进行态（`IsFailed=false`，`PhaseText` 为三阶段文案之一）与失败态（`IsFailed=true`，`PhaseText=SplashPhaseFailed`，`ErrorMessage` 非空）。`OnProgress` 第 40 行每次先把 `IsFailed` 复位，失败后来新进度即恢复进行态。

### 与外部类型的对应关系

| 本模块类型 | 实现/消费的抽象（定义处） |
|---|---|
| `DashBoardModule` | `Prism.Modularity.IModule`（Prism.DryIoc 传递） |
| 接口贡献类（`DashBoardStatusBarItem`） | `Core/Abstractions/Contributions/` 下 `IStatusBarItemContribution` 接口（详见 docs/analysis/Core/Abstractions/api.md） |
| `DashBoardWindowViewModel` 的事件订阅/发布 | `StartupProgressEvent`/`ModuleLoadFailedEvent`/`StartupFailureActionEvent`（Core/Models/Events/，详见 docs/analysis/Core/Models/） |
| `DashBoardWindow` 的显示方 | shell 启动序列直接 `Container.Resolve<DashBoardWindow>()` 显示（`WorkstationApplication.CreateSplashWindow`）；模块内不再有重开通路（原菜单/命令贡献已删除） |
