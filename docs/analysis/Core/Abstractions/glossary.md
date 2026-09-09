# Abstractions — 术语表

## 领域术语与缩写

| 术语 | 定义 | 首次出现位置 |
|---|---|---|
| **Shell** | 应用主窗口的整体界面框架（导航栏 + 侧边栏 + 主内容 + 面板 + 菜单 + 状态栏的组合容器），非操作系统 shell | Regions/ShellRegions.cs 注释「Shell 布局的 Prism Region 名称常量」 |
| **Region（Prism Region）** | Prism 框架的 UI 区域占位机制：XAML 中命名一个区域，运行期由模块把视图注入该区域。本模块以 `ShellRegions` 常量声明五个 Region 名 | Regions/ShellRegions.cs |
| **ActivityBar** | 工作区最左侧的竖向导航栏（VS Code 风格） | Regions/ShellRegions.cs `ActivityBar` 常量 |
| **SideBar** | ActivityBar 右侧的容器，显示当前选中导航项的内容 | Regions/ShellRegions.cs `SideBar` 常量 |
| **MainContent** | 工作区中央的主 Region，单视图切换（同一时刻只显示一个主视图） | Regions/ShellRegions.cs `MainContent` 常量 |
| **AuxiliaryPanel** | 工作区右侧的 tab + 容器区域 | Regions/ShellRegions.cs `AuxiliaryPanel` 常量 |
| **BottomPanel** | 工作区底部的 tab + 容器区域 | Regions/ShellRegions.cs `BottomPanel` 常量 |
| **Contribution（贡献）** | 本模块的核心模式：模块向 shell 贡献 UI 条目，shell 收集后渲染。接口类贡献三种：主视图、菜单项、状态栏项；导航项与面板 tab 已合并为**工具视图**（attribute 声明 + 扫描生成元数据，ADR-0002）。菜单项通常不手写实现，由 `MenuGroupAttribute`/`MenuItemAttribute` 标注的菜单类经 `RegisterMenus` 扫描生成（ADR-0001） | `I*Contribution` 接口与 `ToolViewContribution`（Contributions/ 与 Menus/ 目录） |
| **工具视图（Tool View）** | 带图标与标题的可停靠界面单元，统一取代原导航项/面板 tab 两契约（ADR-0002，docs/adr/0002-toolview-drag-persistence.md）：View 类标 `ToolViewAttribute`，经 Framework 侧 `RegisterToolViews(Assembly)` 扫描生成 `ToolViewContribution` 元数据并注册 View 类型；`Default` 是默认归属，用户拖拽后的实际归属以持久化布局为准 | Contributions/ToolViewAttribute.cs、Contributions/ToolViewContribution.cs |
| **钉住项（Pinned Item）** | `AllowMove = false` 且 `Default = ActivityBar` 的工具视图（如设置），固定渲染在 ActivityBar 底部段，不参与拖拽（ADR-0002 第 2 条） | Contributions/ToolViewAttribute.cs `AllowMove` 注释 |
| **Placement（定位）** | 工具视图默认栖身 Bar 的枚举 `ToolViewPlacement`（`ActivityBar`/`AuxiliaryPanel`/`BottomPanel`）；菜单无定位枚举，用菜单路径/菜单组定位 | Contributions/ToolViewAttribute.cs 第 6-22 行 |
| **菜单路径（Menu Path）** | 菜单项在菜单栏中的位置，以 "/" 分隔的多级名称（各段为 Language 资源键）。首段是顶层菜单；中间各段是子菜单节点；路径不限深度（与根 CONTEXT.md 一致） | Menus/IMenuItemContribution.cs `Path` 属性、Menus/MenuGroupAttribute.cs 构造参 |
| **菜单组（Menu Group）** | 同一父菜单内、由分隔线（Separator）隔开的命名分区。不同组之间自动插入分隔线；组按 GroupOrder 排序，组内条目按 Order 排序。顶层菜单（菜单栏本身）不分组、不插分隔线，只按 Order 排序。未指定组的条目归入无名默认组，默认组排在所有命名组之前（与根 CONTEXT.md 一致） | Menus/IMenuItemContribution.cs `Group`/`GroupOrder` 属性 |
| **Order** | 排序权重，小者靠前；作用域为同一 Bar/面板内。菜单契约另有两级位次：`GroupOrder`（组间）与 `NodeOrder`（顶层菜单/末端子菜单节点在父级中） | 各贡献契约 `Order` 属性 |
| **IconPath** | 图标的 `StreamGeometry` path 字符串（非文件路径、非资源 key），由 `PathIcon` 消费并随主题变色；`ToolViewAttribute` 上对应属性名为 `Icon` | 各贡献契约 `IconPath` 属性、Contributions/ToolViewAttribute.cs `Icon` |
| **ViewType** | 激活贡献项时要显示的视图的 `System.Type`，经容器解析以支持 DI。`ToolViewContribution.ViewType` 与 `IMainViewContribution.ViewType` 命名已统一（原 `ContentViewType` 随导航项/面板 tab 接口删除而不复存在，ADR-0002） | Contributions/ToolViewContribution.cs 第 44 行、Contributions/IMainViewContribution.cs 第 19 行 |
| **OpenMainViewEvent** | shell 侧事件（不在本程序集）：SideBar 内交互请求打开主视图，负载为 `IMainViewContribution.Id` | Contributions/IMainViewContribution.cs 注释 |
| **ShellLayoutState** | shell 侧类型（不在本程序集）：管理面板展开/收起状态，面板收起期间拒绝其 tab 的激活 | Contributions/ToolViewContribution.cs 注释 |
| **IContainerRegistry（Prism）** | Prism 的 DI 注册接口，模块在此注册贡献（接口类以接口注册；工具视图/菜单经 `RegisterToolViews`/`RegisterMenus` 扩展扫描注册）；本程序集未引用 Prism，仅注释提及 | 各 `I*Contribution` 接口注释 |

## 与同名通用概念的区别

- **Shell ≠ 命令行 shell**：指 Avalonia 桌面应用的主窗口界面框架。
- **Window 管理 ≠ 操作系统窗口管理器**：`IWindowManager`（WindowManager/IWindowManager.cs）只管本应用内 Avalonia `Window` 的显示/隐藏/关闭，窗口实例来自 DI 容器而非 `new`。
- **Id ≠ 数据库主键**：是贡献项的稳定字符串标识，供 shell 索引与事件负载匹配；唯一性范围因契约而异（均为全局唯一，约定模块名前缀），见 pitfalls.md。
- **Dialog（ShowDialog）**：`IWindowManager.ShowDialog` 系列指模态对话框式显示窗口，与 `ShowWindow` 非模态显示相对（接口注释层面；模态语义由实现侧保证）。

## 代码命名 ↔ 领域词汇对应

| 代码元素 | 业务概念 |
|---|---|
| `ToolViewAttribute` / `ToolViewContribution` | 工具视图：ActivityBar 导航项或右/底面板 tab 页的统一声明（attribute）与元数据（扫描产物） |
| `IMainViewContribution` | 可在 MainContent 打开的一个主视图（如 dashboard.overview） |
| `IMenuItemContribution` | 菜单栏中经菜单路径/菜单组定位的一个菜单项 |
| `IStatusBarItemContribution` | 状态栏一个「图标 + 文本」状态指示 |
| `IWindowManager` / `IMainWindowManager` | 应用窗口显隐服务 / 主窗口（托盘显隐类场景）专属服务 |
| `WindowManagerExtenstion`（源码拼写如此） | `IWindowManager` 的泛型调用糖 |
