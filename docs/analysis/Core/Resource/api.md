# Core/Resource — 对外接口与调用方式

## 公开 API 面

模块只公开一个类型：`DigitalWorkstation.Core.Resource.Language`（`Core/Resource/Language.cs:9`），`public static class`。

### 方法

| 签名 | 说明 |
|---|---|
### 静态只读属性（27 个，全部以 `Get(nameof(属性名))` 实现，返回 `string`）


定义于 `Language.cs:25-153`。键名 = 属性名，中文值来自 `Language.resx`，英文值来自 `Language.en-US.resx`：

| 属性 | 中文值 | 英文值 | 用途（resx comment） |
|---|---|---|---|
| `SettingsNavigationTitle`（:25） | 设置 | Settings | shell 预置"设置"导航项标题 |
| `DashBoardNavigationTitle`（:30） | 启动台 | Launch Pad | DashBoard 导航项标题 |
| `PropertiesTabTitle`（:34） | 属性 | Properties | shell 预置 AuxiliaryPanel 演示 tab"属性"标题 |
| `OutlineTabTitle`（:39） | 大纲 | Outline | shell 预置 AuxiliaryPanel 演示 tab"大纲"标题 |
| `OutputTabTitle`（:44） | 输出 | Output | shell 预置 BottomPanel 演示 tab"输出"标题 |
| `LogTabTitle`（:49） | 日志 | Log | shell 预置 BottomPanel 演示 tab"日志"标题 |
| `DashBoardTasksTabTitle`（:54） | 任务 | Tasks | DashBoard 贡献给 BottomPanel 的演示 tab"任务"标题 |
| `MenuFileTitle`（:58） | 文件 | File | 顶层"文件"菜单标题 |
| `MenuViewTitle`（:63） | 视图 | View | 顶层"视图"菜单标题 |
| `MenuHelpTitle`（:68） | 帮助 | Help | 顶层"帮助"菜单标题 |
| `MenuExitTitle`（:73） | 退出 | Exit | 文件菜单"退出"项标题 |
| `MenuAboutTitle`（:78） | 关于 | About | 帮助菜单"关于"项标题 |
| `ToggleSideBarTitle`（:83） | 切换 SideBar | Toggle Side Bar | SideBar 显隐切换项（视图菜单） |
| `ToggleBottomPanelTitle`（:88） | 切换 BottomPanel | Toggle Bottom Panel | BottomPanel 显隐切换项（视图菜单） |
| `ToggleAuxiliaryPanelTitle`（:93） | 切换 AuxiliaryPanel | Toggle Auxiliary Panel | AuxiliaryPanel 显隐切换项（视图菜单） |
| `PanelAlignLeftTitle`（:98） | 左对齐 | Align Left | 面板对齐菜单"左对齐"项标题 |
| `PanelAlignRightTitle`（:103） | 右对齐 | Align Right | 面板对齐菜单"右对齐"项标题 |
| `PanelAlignCenterTitle`（:108） | 居中 | Align Center | 面板对齐菜单"居中"项标题 |
| `PanelAlignJustifyTitle`（:113） | 两端对齐 | Justify | 面板对齐菜单"两端对齐"项标题 |
| `ResetLayoutTitle`（:118） | 重置布局 | Reset Layout | 视图菜单"重置布局"项标题 |
| `StatusReadyTitle`（:123） | 就绪 | Ready | shell 预置状态栏"就绪"项文本 |
| `DashBoardOpenWindowMenuTitle`（:128） | 打开启动台 | Open Launch Pad | DashBoard 贡献给文件菜单的"打开启动台"项标题 |
| `SplashStartingText`（:133） | 正在启动… | Starting… | 启动台显示进度前的初始阶段文本 |
| `SplashPhaseCoreServices`（:138） | 初始化核心服务 | Initializing core services | 启动台阶段名 |
| `SplashPhaseLoadingModules`（:143） | 加载模块 | Loading modules | 启动台阶段名 |
| `SplashPhaseReady`（:148） | 就绪 | Ready | 启动台阶段名 |
| `SplashPhaseFailed`（:153） | 模块加载失败 | Module failed to load | 启动台阶段名 |


> 注意：`StatusReadyTitle`（状态栏"就绪"，:123）与 `SplashPhaseReady`（启动画面"就绪"阶段名，:148）**中文值同为"就绪"，但用途不同，是两个独立的键**，不能合并。
### 内部（非公开）成员
- `private static readonly ResourceManager Manager`（`Language.cs:11-12`）：基名 `"DigitalWorkstation.Core.Resource.Language"`，绑定 `typeof(Language).Assembly`。
- `Manager` 每次 `GetString` 都按调用线程的 `CultureInfo.CurrentUICulture` 解析：先找 en-US 卫星资源（`Language.en-US.resx` 编译产物），找不到/未命中则回退中性资源（`Language.resx` 中文）。本模块自身不提供切换语言的 API。

## 调用方式
典型调用模式一——给 shell 贡献项（导航项/面板 tab/状态栏项）的 `Title` 属性供值：

```csharp
using DigitalWorkstation.Core.Resource;
// Modules/Workstation/Contributions/SettingsNavigationItem.cs:15
public string Title => Language.SettingsNavigationTitle;
```

典型调用模式二——菜单项不再持有 `Title` 属性，改为在 Attribute 里携带 Language 资源键字符串，由 Framework 的 `MenuRegistration`/`MenuTreeBuilder` 经 `Language.Get` 解析（详见 Core/Framework 文档）：

```csharp
// Modules/Workstation/Menus/FileMenus.cs:18
[MenuItem("MenuExitTitle", Order = 100, Icon = Icons.Exit)]
public void Exit() { ... }
```

真实调用点（`grep` 全仓库验证）：

强类型属性：

- `Modules/Workstation/Contributions/SettingsNavigationItem.cs:15` → `Language.SettingsNavigationTitle`
- `Modules/Workstation/Contributions/ReadyStatusBarItem.cs:14` → `Language.StatusReadyTitle`
- `Modules/Workstation/Contributions/{Properties,Outline,Output,Log}PanelTab.cs:15` → 对应 Tab 标题属性
- `Modules/DashBoard/DashBoardNavigationItem.cs:15`、`DashBoardStatusBarItem.cs:15` → `Language.DashBoardNavigationTitle`
- `Modules/DashBoard/DashBoardTasksPanelTab.cs:15` → `Language.DashBoardTasksTabTitle`
- `Modules/DashBoard/ViewModels/Windows/DashBoardWindowViewModel.cs:24,43-45,56` → Splash 系列 5 个属性（启动画面阶段文案）

Attribute 字符串键（经 Framework 的 `Language.Get` 间接解析）：

- `Modules/Workstation/Menus/FileMenus.cs:12,18` → `[MenuGroup("MenuFileTitle", ...)]`、`[MenuItem("MenuExitTitle", ...)]`
- `Modules/Workstation/Menus/ViewPanelMenus.cs:11,14,20,26` → `MenuViewTitle` + Toggle 系列 3 键
- `Modules/Workstation/Menus/ViewAlignmentMenus.cs:12,15,21,27,33` → `MenuViewTitle` + PanelAlign 系列 4 键
- `Modules/Workstation/Menus/ViewLayoutMenus.cs:10,13` → `MenuViewTitle` + `ResetLayoutTitle`
- `Modules/Workstation/Menus/HelpMenus.cs:11,17` → `[MenuGroup("MenuHelpTitle", ...)]`、`[MenuItem("MenuAboutTitle", ...)]`
- `Modules/DashBoard/DashBoardMenus.cs:11,17` → `[MenuGroup("MenuFileTitle", ...)]`、`[MenuItem("DashBoardOpenWindowMenuTitle", ...)]`

`Language.Get(string)` 直接调用点（ADR-0001 菜单重构后首次有了真实消费方，不再只是给将来动态键场景留的后门）：

- `Core/Framework/Menus/MenuRegistration.cs:85` → `Language.Get(item.Title)`：把 `MenuItemAttribute` 携带的 title 键解析为当前 UI 区域性下的显示标题
- `Core/Framework/Menus/MenuTreeBuilder.cs:59,74,98` → `Language.Get(node.Segment)`：把菜单路径各段键解析为顶层/子菜单标题，并作为顶层与子菜单分组的排序键

## 对外公开的数据结构

无自定义数据结构。输入输出都是 `string`；语言资源键的"结构"即上表 27 个键，物理载体是两个 resx 文件中的 `<data name="键名"><value>文案</value><comment>用途</comment></data>` 条目（`Language.resx:61-168`、`Language.en-US.resx:61-168`）。
