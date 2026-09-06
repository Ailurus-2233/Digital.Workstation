# Models — 模块关系链

## 依赖关系（本模块 → 外部）

来自 `Core/Models/Models.csproj`：

### 项目引用

| 依赖 | 路径 | 用途 |
|---|---|---|
| `Common` | `..\Common\Common.csproj`（Models.csproj 第 10 行） | **本模块源码不引用 Common 的任何类型**（不用 `Logger`，不用 `IoC`）。该引用的唯一实际作用是经 ProjectReference 传递获得 Prism 程序集引用与 Prism global using（见下）。删除它会导致 `PubSubEvent<T>` 无法解析——虽然没有任何 Common 类型被用到。 |

### 传递性 NuGet 依赖（经 Common → Prism.Avalonia / Prism.DryIoc.Avalonia 9.0.537.11130 传入）

事件类的基类 `Prism.Events.PubSubEvent<T>` 来自 Prism 包。`Core/Models/obj/Debug/Models.GlobalUsings.g.cs` 第 2-11 行含 Prism 包 build props 注入的 global using（`Prism`、`Prism.Events`、`Prism.Ioc` 等），其中**第 6 行 `global using Prism.Events;`** 就是 10 个源码文件不写任何 `using` 也能解析 `PubSubEvent<T>` 的原因。

### 编译设置

`<TargetFramework>net10.0</TargetFramework>`、`<ImplicitUsings>enable</ImplicitUsings>`、`<Nullable>enable</Nullable>`（Models.csproj 第 4-6 行）。程序集名/根命名空间由 `Build/Base.props` 的 `_InCore` 分支统一为 `DigitalWorkstation.Core.Models`（Base.props 第 39-42 行）；源码内显式声明的命名空间是 `DigitalWorkstation.Core.Models.Events`（根命名空间 + `Events` 目录段）。输出路径：Debug 落 `$(SolutionDir)Output\Debug\`（Base.props 第 61-63 行），Release 落 `$(SolutionDir)Output\Release\core\`（第 49-51 行）。

### 文档引用（非编译依赖）

- **ADR-0004**：`StartupProgressEvent`、`ModuleLoadFailedEvent`、`StartupPhase` 的注释及 README 第 47-51 行均引用之。**该 ADR 文档不在仓库中**（docs/ 下无 adr 目录，全仓 grep 仅见引用、不见定义），决策原文待进一步调查；当前可从注释还原其要点：抑制 Prism 同步 `InitializeModules`，改由启动序列逐模块异步加载，进度与失败决策经事件发布给启动台。

## 被依赖关系（外部 → 本模块）

直接引用 Models.csproj 的项目只有一个；其余消费方经传递引用获得类型：

| 消费方 | 引用方式 | 消费点 |
|---|---|---|
| `Core/Framework`（Framework.csproj 第 12 行直接引用） | 直接 | `FrameworkApplication.cs`：发布 `StartupProgressEvent`（第 69/74/85/104 行）、发布 `ModuleLoadFailedEvent`（第 93-94 行）、在 `WaitForFailureActionAsync` 订阅 `StartupFailureActionEvent`（第 118-125 行）——本模块全部启动事件的唯一发布中枢 |
| `Modules/DashBoard`（DashBoard.csproj 经 Framework 传递引用） | 传递 | `ViewModels/Windows/DashBoardWindowViewModel.cs`：订阅 `StartupProgressEvent`/`ModuleLoadFailedEvent`（第 19-20 行），发布 `StartupFailureActionEvent`（第 72/81 行）；`Views/DashBoardNavigationView.axaml.cs`：发布 `OpenMainViewEvent`（第 30/35 行） |
| `Modules/Workstation`（Workstation.csproj 经 Framework 传递引用） | 传递 | `MainWindowViewModel.cs`：订阅 `OpenMainViewEvent`/`TogglePanelVisibilityEvent`（第 32-33 行），`TogglePanel` 消费 `TogglePanelTarget`（第 249-256 行）；`WorkstationApplication.cs` 第 37 行遍历 `TogglePanelTarget` 注册菜单贡献；`Shell/TogglePanelContribution.cs` 发布 `TogglePanelVisibilityEvent`（第 19 行） |
| `Core/Abstractions` | **注释引用，无编译依赖** | `Shell/IMainViewContribution.cs` 第 6 行注释提及 `OpenMainViewEvent` 负载为 `Id`；Abstractions 不引用 Models，`<see cref>` 无法解析——注释是写给实现侧的约定 |
| `Launcher` | 传递（Launcher → Workstation → Framework → Models） | 不直接消费事件类型 |

`UnitTest/Framework` 不引用也不测试本模块任何类型（全仓 grep 无命中），见 testing.md。

## 核心内部数据结构

本模块无 internal 类型，全部 10 个 public 类型的关系图：

```
Prism.Events.PubSubEvent<T>（Prism 包）
  ├─ StartupProgressEvent        ──负载──▶ StartupProgress (record)
  │                                            └─ Phase: StartupPhase (enum: CoreServices/LoadingModules/Ready)
  ├─ ModuleLoadFailedEvent       ──负载──▶ ModuleLoadFailure (record)
  ├─ StartupFailureActionEvent   ──负载──▶ StartupFailureAction (enum: Continue/Exit)
  ├─ OpenMainViewEvent           ──负载──▶ string（主视图 Id = IMainViewContribution.Id）
  └─ TogglePanelVisibilityEvent  ──负载──▶ TogglePanelTarget (enum: SideBar/AuxiliaryPanel/BottomPanel)
```

| 类型 | 种类 | 文件 | 角色 |
|---|---|---|---|
| `StartupProgressEvent` | class : `PubSubEvent<StartupProgress>`（无成员体） | Events/StartupProgressEvent.cs | 启动进度事件 |
| `StartupProgress` | 位置 record（4 参数） | Events/StartupProgress.cs | 启动进度负载 |
| `StartupPhase` | enum（3 成员） | Events/StartupPhase.cs | 启动阶段 |
| `ModuleLoadFailedEvent` | class : `PubSubEvent<ModuleLoadFailure>`（无成员体） | Events/ModuleLoadFailedEvent.cs | 模块加载失败事件 |
| `ModuleLoadFailure` | 位置 record（4 参数） | Events/ModuleLoadFailure.cs | 失败详情负载 |
| `StartupFailureActionEvent` | class : `PubSubEvent<StartupFailureAction>`（无成员体） | Events/StartupFailureActionEvent.cs | 用户决策事件 |
| `StartupFailureAction` | enum（Continue/Exit） | Events/StartupFailureAction.cs | 决策枚举 |
| `OpenMainViewEvent` | class : `PubSubEvent<string>`（无成员体） | Events/OpenMainViewEvent.cs | 打开主视图请求 |
| `TogglePanelVisibilityEvent` | class : `PubSubEvent<TogglePanelTarget>`（无成员体） | Events/TogglePanelVisibilityEvent.cs | 面板显隐请求 |
| `TogglePanelTarget` | enum（3 成员） | Events/TogglePanelTarget.cs | 目标面板枚举 |

跨程序集耦合点：`OpenMainViewEvent` 的负载 string 与 `Core/Abstractions/Shell/IMainViewContribution.Id` 构成**字符串级契约**——Id 值必须精确匹配（如 `DashBoardOverviewMainView.ViewId`），不匹配则 shell 找不到贡献，静默无反应。
