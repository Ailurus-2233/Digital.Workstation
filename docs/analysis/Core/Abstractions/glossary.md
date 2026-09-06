# Abstractions — 术语表

## 领域术语与缩写

| 术语 | 定义 | 首次出现位置 |
|---|---|---|
| **Shell** | 应用主窗口的整体界面框架（导航栏 + 侧边栏 + 主内容 + 面板 + 菜单 + 状态栏的组合容器），非操作系统 shell | Shell/ShellRegions.cs 注释「Shell 布局的 Prism Region 名称常量」 |
| **Region（Prism Region）** | Prism 框架的 UI 区域占位机制：XAML 中命名一个区域，运行期由模块把视图注入该区域。本模块以 `ShellRegions` 常量声明五个 Region 名 | Shell/ShellRegions.cs |
| **ActivityBar** | 工作区最左侧的竖向导航栏（VS Code 风格） | Shell/ShellRegions.cs `ActivityBar` 常量 |
| **SideBar** | ActivityBar 右侧的容器，显示当前选中导航项的内容 | Shell/ShellRegions.cs `SideBar` 常量 |
| **MainContent** | 工作区中央的主 Region，单视图切换（同一时刻只显示一个主视图） | Shell/ShellRegions.cs `MainContent` 常量 |
| **AuxiliaryPanel** | 工作区右侧的 tab + 容器区域 | Shell/ShellRegions.cs `AuxiliaryPanel` 常量 |
| **BottomPanel** | 工作区底部的 tab + 容器区域 | Shell/ShellRegions.cs `BottomPanel` 常量 |
| **Contribution（贡献）** | 本模块的核心模式：模块实现一个 `I*Contribution` 接口并在 DI 容器注册，shell 收集全部实现渲染 UI。共五种：导航项、主视图、面板 tab、菜单项、状态栏项 | 五个 `I*Contribution` 接口（Shell/ 目录） |
| **Placement（定位）** | 贡献项落在哪个区域的三组枚举：`NavigationItemPlacement`（Top/Bottom）、`PanelPlacement`（Auxiliary/Bottom）、`MenuPlacement`（File/View/Help） | 各自接口文件顶部 |
| **Order** | 排序权重，小者靠前；作用域为同一 Placement/面板/菜单内 | 各 `I*Contribution` 接口 `Order` 属性 |
| **IconPath** | 图标的 `StreamGeometry` path 字符串（非文件路径、非资源 key），由 `PathIcon` 消费并随主题变色 | 各 `I*Contribution` 接口 `IconPath` 属性 |
| **ContentViewType / ViewType** | 激活贡献项时要显示的视图的 `System.Type`，经容器解析以支持 DI。命名不统一：`IMainViewContribution` 叫 `ViewType`，`INavigationItemContribution`/`IPanelTabContribution` 叫 `ContentViewType` | Shell/IMainViewContribution.cs、Shell/INavigationItemContribution.cs |
| **OpenMainViewEvent** | shell 侧事件（不在本程序集）：SideBar 内交互请求打开主视图，负载为 `IMainViewContribution.Id` | Shell/IMainViewContribution.cs 注释 |
| **ShellLayoutState** | shell 侧类型（不在本程序集）：管理面板展开/收起状态，面板收起期间拒绝其 tab 的激活 | Shell/IPanelTabContribution.cs 注释 |
| **IContainerRegistry（Prism）** | Prism 的 DI 注册接口，模块在此以贡献接口注册实现；本程序集未引用 Prism，仅注释提及 | 各 `I*Contribution` 接口注释 |

## 与同名通用概念的区别

- **Shell ≠ 命令行 shell**：指 Avalonia 桌面应用的主窗口界面框架。
- **Window 管理 ≠ 操作系统窗口管理器**：`IWindowManager`（WindowManager/IWindowManager.cs）只管本应用内 Avalonia `Window` 的显示/隐藏/关闭，窗口实例来自 DI 容器而非 `new`。
- **Id ≠ 数据库主键**：是贡献项的稳定字符串标识，供 shell 索引与事件负载匹配；唯一性范围因接口而异（模块内 / 跨面板 / 全局），见 pitfalls.md。
- **Dialog（ShowDialog）**：`IWindowManager.ShowDialog` 系列指模态对话框式显示窗口，与 `ShowWindow` 非模态显示相对（接口注释层面；模态语义由实现侧保证）。

## 代码命名 ↔ 领域词汇对应

| 代码元素 | 业务概念 |
|---|---|
| `INavigationItemContribution` | ActivityBar 上一个功能域/设置入口（点击后 SideBar 换内容） |
| `IMainViewContribution` | 可在 MainContent 打开的一个主视图（如 dashboard.overview） |
| `IPanelTabContribution` | 右侧或底部面板中的一个 tab 页 |
| `IMenuItemContribution` | 菜单栏 File/View/Help 下的一个菜单项 |
| `IStatusBarItemContribution` | 状态栏一个「图标 + 文本」状态指示 |
| `IWindowManager` / `IMainWindowManager` | 应用窗口显隐服务 / 主窗口（托盘显隐类场景）专属服务 |
| `WindowManagerExtenstion`（源码拼写如此） | `IWindowManager` 的泛型调用糖 |
