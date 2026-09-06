# Core/Resource — 模块关系链

## 依赖关系（本模块 → 外部）

**零项目依赖、零 NuGet 包依赖。** `Core/Resource/Resource.csproj` 全文只有三个属性：`<ImplicitUsings>enable</ImplicitUsings>`、`<Nullable>enable</Nullable>`、`<TargetFramework>net10.0</TargetFramework>`，没有任何 `<ProjectReference>` 或 `<PackageReference>`。

仅依赖 .NET 10 BCL 的一个命名空间：

- `System.Resources.ResourceManager`（`Language.cs:1,11-12`）：本地化资源查找与回退的核心引擎。模块的全部"本地化能力"都来自它——按 `CultureInfo.CurrentUICulture` 探测卫星程序集（en-US），探测不到回退主程序集内嵌的中性资源（中文）。

构建产物：`Resource.dll` 主程序集（内嵌中性资源 `DigitalWorkstation.Core.Resource.Language.resources`）+ `en-US/Resource.resources.dll` 卫星程序集（由 `Language.en-US.resx` 编译）。消费方项目引用本模块后，卫星程序集随引用拷贝到输出目录。

## 被依赖关系（外部 → 本模块）

两个项目通过 `<ProjectReference Include="..\..\Core\Resource\Resource.csproj" />` 引用本模块（解决方案 `Digital.Workstation.slnx:7` 把它列在 `/Core/` 文件夹下）：

### 1. `Modules/Workstation/Workstation.csproj`（:11）— 宿主 shell

场景：shell 预置的界面贡献项取标题文案。`using DigitalWorkstation.Core.Resource;` 出现在：

- `Modules/Workstation/Shell/SettingsNavigationItem.cs:15` — 设置导航项
- `Modules/Workstation/Shell/AboutMenuItem.cs:22` — 帮助菜单"关于"
- `Modules/Workstation/Shell/ExitMenuItem.cs:18` — 文件菜单"退出"
- `Modules/Workstation/Shell/ReadyStatusBarItem.cs:14` — 状态栏"就绪"
- `Modules/Workstation/Shell/PropertiesPanelTab.cs:15`、`OutlinePanelTab.cs:15` — AuxiliaryPanel 演示 tab
- `Modules/Workstation/Shell/OutputPanelTab.cs:15`、`LogPanelTab.cs:15` — BottomPanel 演示 tab
- `Modules/Workstation/Shell/TogglePanelContribution.cs:31-33` — 视图菜单三个面板显隐切换项：switch 表达式中 `TogglePanelTarget.SideBar`/`AuxiliaryPanel` 是显式 case，`BottomPanel` 走弃元默认分支 `_ => Language.ToggleBottomPanelTitle`

### 2. `Modules/DashBoard/DashBoard.csproj`（:22）— 启动台模块

场景：DashBoard 模块贡献的界面项标题 + 启动画面（splash）阶段文案：

- `Modules/DashBoard/DashBoardNavigationItem.cs:15`、`DashBoardStatusBarItem.cs:15` — 导航项/状态栏项"启动台"
- `Modules/DashBoard/DashBoardTasksPanelTab.cs:15` — BottomPanel"任务"tab
- `Modules/DashBoard/OpenDashBoardMenuItem.cs:23` — 文件菜单"打开启动台"
- `Modules/DashBoard/ViewModels/Windows/DashBoardWindowViewModel.cs:24,43-45,56` — 启动画面：`SplashStartingText` 作 `_phaseText` 初值；按 `StartupPhase` 枚举在 `SplashPhaseCoreServices`/`SplashPhaseLoadingModules`/`SplashPhaseReady` 间切换；失败时置 `SplashPhaseFailed`

`Core/Models`、`Core/Abstractions`、`Core/Framework`、`Core/UIPackage` 均**不**引用本模块。

## 核心内部数据结构

模块极小，只有一层结构：

| 类型/成员 | 定义位置 | 说明 |
|---|---|---|
| `static class Language` | `Core/Resource/Language.cs:9` | 唯一公开类型，命名空间 `DigitalWorkstation.Core.Resource` |
| `Manager: ResourceManager`（私有静态只读） | `Language.cs:11-12` | 基名 `"DigitalWorkstation.Core.Resource.Language"`；该基名 = 程序集默认命名空间 + resx 文件名（不含扩展名），与 `Language.resx` 的编译逻辑名严格对应 |
| `Get(string key): string` | `Language.cs:17-20` | 所有取值的唯一 funnel：`Manager.GetString(key) ?? key` |
| 19 个静态属性 | `Language.cs:25-113` | 每个都是 `Get(nameof(属性名))` 的转发；**键与属性同名是本模块的核心不变量** |
| 资源条目（19 个 `<data>`） | `Language.resx:61-136`（中性/中文）、`Language.en-US.resx:61-136`（英文） | 两个 resx 的键集合完全一一对应；每条带 `<comment>` 说明用途 |

关系：`Language.属性 → Get → ResourceManager → (en-US 卫星 | 中性回退) → <data> 条目`。命名空间 `DigitalWorkstation.Core.Resource` + 文件名 `Language.resx` 共同决定了 `ResourceManager` 基名，三者任一改动都会破坏资源清单查找（见 pitfalls.md）。
