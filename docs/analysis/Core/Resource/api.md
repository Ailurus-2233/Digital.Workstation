# Core/Resource — 对外接口与调用方式

## 公开 API 面

模块只公开一个类型：`DigitalWorkstation.Core.Resource.Language`（`Core/Resource/Language.cs:9`），`public static class`。

### 方法

| 签名 | 说明 |
|---|---|
### 静态只读属性（全部以 `Get(nameof(属性名))` 实现，返回 `string`）


定义于 `Language.cs:25-168`。键名 = 属性名，中文值来自 `Language.resx`，英文值来自 `Language.en-US.resx`：

| 属性 | 中文值 | 英文值 | 用途（resx comment） |
|---|---|---|---|
| `SettingsNavigationTitle`（:25） | 设置 | Settings | shell 预置"设置"导航项标题 |
| `DashBoardNavigationTitle`（:30） | 启动台 | Launch Pad | DashBoard 导航项标题 |
| `MenuFileTitle`（:34） | 文件 | File | 顶层"文件"菜单标题 |
| `MenuViewTitle`（:39） | 视图 | View | 顶层"视图"菜单标题 |
| `MenuHelpTitle`（:44） | 帮助 | Help | 顶层"帮助"菜单标题 |
| `MenuExitTitle`（:49） | 退出 | Exit | 文件菜单"退出"项标题 |
| `ReturnHomeTitle` | 回到主页 | Go Home | 回到主页命令与文件菜单项标题 |
| `MenuPreferencesTitle` | 首选项 | Preferences | 文件菜单项标题，打开现有设置页 |
| `MenuAboutTitle`（:54） | 关于 | About | 帮助菜单"关于"项标题 |
| `ToggleSideBarTitle`（:59） | 切换 SideBar | Toggle Side Bar | SideBar 显隐切换项（视图菜单） |
| `ToggleBottomPanelTitle`（:64） | 切换 BottomPanel | Toggle Bottom Panel | BottomPanel 显隐切换项（视图菜单） |
| `ToggleAuxiliaryPanelTitle`（:69） | 切换 AuxiliaryPanel | Toggle Auxiliary Panel | AuxiliaryPanel 显隐切换项（视图菜单） |
| `PanelAlignLeftTitle`（:74） | 左对齐 | Align Left | 面板对齐菜单"左对齐"项标题 |
| `PanelAlignRightTitle`（:79） | 右对齐 | Align Right | 面板对齐菜单"右对齐"项标题 |
| `PanelAlignCenterTitle`（:84） | 居中 | Align Center | 面板对齐菜单"居中"项标题 |
| `PanelAlignJustifyTitle`（:89） | 两端对齐 | Justify | 面板对齐菜单"两端对齐"项标题 |
| `ResetLayoutTitle`（:94） | 重置布局 | Reset Layout | 视图菜单"重置布局"项标题 |
| `StatusReadyTitle`（:99） | 就绪 | Ready | shell 预置状态栏"就绪"项文本 |
| `SplashStartingText`（:104） | 正在启动… | Starting… | 启动台显示进度前的初始阶段文本 |
| `SplashPhaseCoreServices`（:109） | 初始化核心服务 | Initializing core services | 启动台阶段名 |
| `SplashPhaseLoadingModules`（:114） | 加载模块 | Loading modules | 启动台阶段名 |
| `SplashPhaseReady`（:119） | 就绪 | Ready | 启动台阶段名 |
| `SplashPhaseFailed`（:123） | 模块加载失败 | Module failed to load | 启动台阶段名 |
| `CommandPaletteWatermark`（:128） | 输入命令以执行 | Type a command to execute | 命令面板输入框水印（ADR-0005） |
| `NoMatchingCommands`（:133） | 无匹配命令 | No matching commands | 命令面板空态文案（ADR-0005） |
| `SettingsGeneralGroupName`（:138） | 常规 | General | 设置页"常规"分组的显示名（Framework 预置设置分组，ADR-0006） |
| `SettingsLanguageName`（:143） | 语言 | Language | 设置项"语言"的显示名（Framework 预置，ADR-0006） |
| `SettingsLanguageNameZhCN`（:148） | 中文（简体） | Chinese (Simplified) | 语言设置项成员 ZhCN 的显示名（键按「设置项名称键 + 成员名」约定生成，ADR-0006 决策 8） |
| `SettingsLanguageNameEnUS`（:153） | English (US) | English (US) | 语言设置项成员 EnUS 的显示名（同上约定） |
| `SettingsRestartPendingMark`（:158） | 重启后生效 | Restart to apply | 设置页需重启设置项被修改后的项级标记文本（ADR-0006 决策 7） |
| `SettingsRestartBannerText`（:163） | 部分设置的更改将在重启后生效 | Some setting changes will take effect after restart | 设置页顶部「存在未生效的需重启修改」横幅文本（ADR-0006 决策 7） |
| `SettingsRestartNowButtonTitle`（:168） | 立即重启 | Restart Now | 设置页重启横幅上「立即重启」按钮的标题（ADR-0006 决策 7） |


> 注意：`StatusReadyTitle`（状态栏"就绪"，:99）与 `SplashPhaseReady`（启动画面"就绪"阶段名，:119）**中文值同为"就绪"，但用途不同，是两个独立的键**，不能合并。
### 内部（非公开）成员
- `private static readonly ResourceManager Manager`（`Language.cs:11-12`）：基名 `"DigitalWorkstation.Core.Resource.Language"`，绑定 `typeof(Language).Assembly`。
- `Manager` 每次 `GetString` 都按调用线程的 `CultureInfo.CurrentUICulture` 解析：先找 en-US 卫星资源（`Language.en-US.resx` 编译产物），找不到/未命中则回退中性资源（`Language.resx` 中文）。本模块自身不提供切换语言的 API。

## 调用方式
典型调用模式一——给 shell 内置条目（状态栏项/设置导航按钮）的 `Title` 属性供值：

```csharp
using DigitalWorkstation.Core.Resource;
// Modules/Workstation/MainWindowViewModel.cs:151
public string SettingsTitle => Language.SettingsNavigationTitle;
```

典型调用模式二——菜单项不再持有 `Title` 属性，改为在 Attribute 里携带 Language 资源键字符串，由 Framework 的 `MenuRegistration`/`MenuTreeBuilder` 经 `Language.Get` 解析（详见 Core/Framework 文档）：

```csharp
// Modules/Workstation/Menus/FileMenus.cs:18
[MenuItem("MenuExitTitle", Order = 100, Icon = Icons.Exit)]
public void Exit() { ... }
```

真实调用点（`grep` 全仓库验证）：

强类型属性：

- `Modules/Workstation/MainWindowViewModel.cs:151` → `Language.SettingsNavigationTitle`（ActivityBar 底部"设置"导航按钮标题，ADR-0006）
- `Modules/Workstation/Contributions/ReadyStatusBarItem.cs:14` → `Language.StatusReadyTitle`
- `Modules/DashBoard/DashBoardStatusBarItem.cs:15` → `Language.DashBoardNavigationTitle`
- `Modules/DashBoard/ViewModels/Windows/DashBoardWindowViewModel.cs:24,43-45,56` → Splash 系列 5 个属性（启动画面阶段文案）
- `Modules/Settings/ViewModels/SettingsPageViewModel.cs` → `Language.SettingsRestartBannerText`/`SettingsRestartNowButtonTitle`（重启横幅文本与按钮标题，ADR-0006 决策 7）
- `Modules/Settings/ViewModels/SettingItemModel.cs` → `Language.SettingsRestartPendingMark`（「重启后生效」项级标记文本，决策 7）

Attribute 字符串键（经 Framework 的 `Language.Get` 间接解析）：

- `Modules/Workstation/Menus/FileMenus.cs:12,18` → `[MenuGroup("MenuFileTitle", ...)]`、`[MenuItem("MenuExitTitle", ...)]`
- `Modules/Workstation/Menus/ViewPanelMenus.cs:11,14,20,26` → `MenuViewTitle` + Toggle 系列 3 键
- `Modules/Workstation/Menus/ViewAlignmentMenus.cs:12,15,21,27,33` → `MenuViewTitle` + PanelAlign 系列 4 键
- `Modules/Workstation/Menus/ViewLayoutMenus.cs:10,13` → `MenuViewTitle` + `ResetLayoutTitle`
- `Modules/Workstation/Menus/HelpMenus.cs:11,17` → `[MenuGroup("MenuHelpTitle", ...)]`、`[MenuItem("MenuAboutTitle", ...)]`

`Language.Get(string)` 直接调用点（ADR-0001 菜单重构后首次有了真实消费方，不再只是给将来动态键场景留的后门）：

- `Core/Framework/Menus/MenuRegistration.cs:85` → `Language.Get(item.Title)`：把 `MenuItemAttribute` 携带的 title 键解析为当前 UI 区域性下的显示标题
- `Core/Framework/Menus/MenuTreeBuilder.cs:59,74,98` → `Language.Get(node.Segment)`：把菜单路径各段键解析为顶层/子菜单标题，并作为顶层与子菜单分组的排序键

## 对外公开的数据结构

无自定义数据结构。输入输出都是 `string`；语言资源键的"结构"即上表 30 个键，物理载体是两个 resx 文件中的 `<data name="键名"><value>文案</value><comment>用途</comment></data>` 条目（`Language.resx:61-180`、`Language.en-US.resx:61-180`）。
