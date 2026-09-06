# Core/Resource — 对外接口与调用方式

## 公开 API 面

模块只公开一个类型：`DigitalWorkstation.Core.Resource.Language`（`Core/Resource/Language.cs:9`），`public static class`。

### 方法

| 签名 | 说明 |
|---|---|
### 静态只读属性（19 个，全部以 `Get(nameof(属性名))` 实现，返回 `string`）


定义于 `Language.cs:25-113`。键名 = 属性名，中文值来自 `Language.resx`，英文值来自 `Language.en-US.resx`：

| 属性 | 中文值 | 英文值 | 用途（resx comment） |
|---|---|---|---|
| `SettingsNavigationTitle`（:25） | 设置 | Settings | shell 预置"设置"导航项标题 |
| `DashBoardNavigationTitle`（:30） | 启动台 | Launch Pad | DashBoard 导航项标题 |
| `PropertiesTabTitle`（:34） | 属性 | Properties | shell 预置 AuxiliaryPanel 演示 tab"属性"标题 |
| `OutlineTabTitle`（:39） | 大纲 | Outline | shell 预置 AuxiliaryPanel 演示 tab"大纲"标题 |
| `OutputTabTitle`（:44） | 输出 | Output | shell 预置 BottomPanel 演示 tab"输出"标题 |
| `LogTabTitle`（:49） | 日志 | Log | shell 预置 BottomPanel 演示 tab"日志"标题 |
| `DashBoardTasksTabTitle`（:54） | 任务 | Tasks | DashBoard 贡献给 BottomPanel 的演示 tab"任务"标题 |
| `MenuExitTitle`（:58） | 退出 | Exit | 文件菜单"退出"项标题 |
| `MenuAboutTitle`（:63） | 关于 | About | 帮助菜单"关于"项标题 |
| `ToggleSideBarTitle`（:68） | 切换 SideBar | Toggle Side Bar | SideBar 显隐切换项（视图菜单） |
| `ToggleBottomPanelTitle`（:73） | 切换 BottomPanel | Toggle Bottom Panel | BottomPanel 显隐切换项（视图菜单） |
| `ToggleAuxiliaryPanelTitle`（:78） | 切换 AuxiliaryPanel | Toggle Auxiliary Panel | AuxiliaryPanel 显隐切换项（视图菜单） |
| `StatusReadyTitle`（:83） | 就绪 | Ready | shell 预置状态栏"就绪"项文本 |
| `DashBoardOpenWindowMenuTitle`（:88） | 打开启动台 | Open Launch Pad | DashBoard 贡献给文件菜单的"打开启动台"项标题 |
| `SplashStartingText`（:93） | 正在启动… | Starting… | 启动台显示进度前的初始阶段文本 |
| `SplashPhaseCoreServices`（:98） | 初始化核心服务 | Initializing core services | 启动台阶段名 |
| `SplashPhaseLoadingModules`（:103） | 加载模块 | Loading modules | 启动台阶段名 |
| `SplashPhaseReady`（:108） | 就绪 | Ready | 启动台阶段名 |
| `SplashPhaseFailed`（:113） | 模块加载失败 | Module failed to load | 启动台阶段名 |


> 注意：`StatusReadyTitle`（状态栏"就绪"，:83）与 `SplashPhaseReady`（启动画面"就绪"阶段名，:108）**中文值同为"就绪"，但用途不同，是两个独立的键**，不能合并。

### 内部（非公开）成员
- `private static readonly ResourceManager Manager`（`Language.cs:11-12`）：基名 `"DigitalWorkstation.Core.Resource.Language"`，绑定 `typeof(Language).Assembly`。
- `Manager` 每次 `GetString` 都按调用线程的 `CultureInfo.CurrentUICulture` 解析：先找 en-US 卫星资源（`Language.en-US.resx` 编译产物），找不到/未命中则回退中性资源（`Language.resx` 中文）。本模块自身不提供切换语言的 API。

## 调用方式

**无初始化/生命周期要求**：静态类，`ResourceManager` 首次访问时由运行时惰性初始化。任何线程可随时调用。

典型调用模式——给 shell 贡献项（菜单项/导航项/面板 tab/状态栏项）的 `Title` 属性供值：

```csharp
using DigitalWorkstation.Core.Resource;
// Modules/Workstation/Shell/ExitMenuItem.cs:18
public string Title => Language.MenuExitTitle;
```

真实调用点（`grep` 全仓库验证）：

- `Modules/Workstation/Shell/SettingsNavigationItem.cs:15` → `Language.SettingsNavigationTitle`
- `Modules/Workstation/Shell/AboutMenuItem.cs:22` → `Language.MenuAboutTitle`
- `Modules/Workstation/Shell/ExitMenuItem.cs:18` → `Language.MenuExitTitle`
- `Modules/Workstation/Shell/ReadyStatusBarItem.cs:14` → `Language.StatusReadyTitle`
- `Modules/Workstation/Shell/{Properties,Outline,Output,Log}PanelTab.cs:15` → 对应 Tab 标题属性
- `Modules/Workstation/Shell/TogglePanelContribution.cs:31-33` → switch 表达式按 `TogglePanelTarget` 取标题：`SideBar => Language.ToggleSideBarTitle`、`AuxiliaryPanel => Language.ToggleAuxiliaryPanelTitle`，**BottomPanel 是弃元默认分支 `_ => Language.ToggleBottomPanelTitle`**
- `Modules/DashBoard/DashBoardNavigationItem.cs:15`、`DashBoardStatusBarItem.cs:15` → `Language.DashBoardNavigationTitle`
- `Modules/DashBoard/DashBoardTasksPanelTab.cs:15` → `Language.DashBoardTasksTabTitle`
- `Modules/DashBoard/OpenDashBoardMenuItem.cs:23` → `Language.DashBoardOpenWindowMenuTitle`
- `Modules/DashBoard/ViewModels/Windows/DashBoardWindowViewModel.cs:24,43-45,56` → Splash 系列 5 个属性（启动画面阶段文案）

所有现有消费方都用强类型属性，**没有消费方直接调用 `Language.Get(string)`**——`Get` 是给将来动态键场景留的后门。

## 对外公开的数据结构

无自定义数据结构。输入输出都是 `string`；语言资源键的"结构"即上表 19 个键，物理载体是两个 resx 文件中的 `<data name="键名"><value>文案</value><comment>用途</comment></data>` 条目（`Language.resx:61-136`、`Language.en-US.resx:61-136`）。
