# 工具视图统一为 ToolView：Attribute 声明、跨 Bar 拖拽、布局持久化

## 状态
已接受

## 上下文

shell 的可停靠内容有两个几乎同构的贡献契约：`IPanelTabContribution`（AuxiliaryPanel/BottomPanel 的 tab）与 `INavigationItemContribution`（ActivityBar 项，内容显示在 SideBar），字段同为 `Id/Title/IconPath/Order/ContentViewType`，唯一实质差异是放置枚举。新需求：(1) 这些条目可在三处 Bar 之间自由拖拽迁移（ActivityBar 底部的设置项除外），布局存为配置文件，启动时恢复；(2) 不再手写贡献类，视图用声明式标记表达「标题、图标、默认位置」。

约束与事实：

- 布局不可拖拽时，归属由贡献的静态 `Panel`/`Placement` 属性决定；可拖拽后归属是用户数据，必须持久化且优先于声明默认值。
- `ShellLayoutState` 是纯 record（string/bool/double/字符串列表），天然可序列化；但仓库此前**零持久化基础设施**（连日志都不落盘），位置/格式/时机无现存约定。
- Avalonia 的 attached property 写在 View 的 XAML 上，只有实例化后才读得到——违背「`ContentViewType` 只给类型、延迟解析」的设计；attribute 标在类上可反射读元数据，零实例化。`Icons.*` 是 `const string` 可直接作 attribute 参数；`Language.*` 是运行时属性，标题只能传资源键。
- 仓库已有 attribute 扫描先例：菜单契约（ADR-0001）由各模块 `RegisterMenus(自己的Assembly)` 按程序集扫描，标题传 `Language` 资源键运行时解析。

备选方案：(B) 拖拽只允许 AuxiliaryPanel ↔ BottomPanel，不动 ActivityBar——不满足「任何 Bar」的诉求，且留着两个同构契约；(C) 保留旧接口、attribute 生成适配器——双机制并存是永久理解负担，违背单一渲染管道原则（同 ADR-0001 否掉 B 的理由）；(D) attached property + 实例化探测——启动时 new 出所有视图，慢且有副作用风险；(E) 不持久化，重启回默认——拖拽功能的价值随之坍塌。

## 决策

1. **统一概念 ToolView（工具视图）**：`IPanelTabContribution` 与 `INavigationItemContribution` 合并删除，替换为标在 View 类上的 `ToolViewAttribute`：`Id`、`TitleKey`（Language 资源键，运行时解析）、`Icon`（`Icons.*` path 常量）、`Default`（`ToolViewPlacement { ActivityBar, AuxiliaryPanel, BottomPanel }`）、`Order`、`AllowMove`（默认 `true`）。`IMainViewContribution`、`IStatusBarItemContribution`、菜单契约不动。
2. **钉住区**：`AllowMove = false` 且 `Default = ActivityBar` 的项（设置）固定渲染在 ActivityBar 底部段，不参与拖拽；底部段不接受拖放，拖拽落点只有顶部段与两个面板。
3. **注册入口**：新增 `RegisterToolViews(Assembly)`，各模块在 `RegisterTypes` 对本程序集调用（`RegisterMenus` 同构）；它扫描 `[ToolView]` 类型生成贡献元数据，并把 View 类型注册进容器——替代手写的 `RegisterSingleton<I*Contribution,…>` 与 `Register<XxxView>()`。不做 AppDomain 全局扫描。
4. **拖拽语义**：同 Bar 拖动按指针位置重排；跨 Bar 插入目标 Bar 指针位置并激活该 tab；面板最后一个 tab 被拖走则自动隐藏，向隐藏面板拖入则自动显示；不提供浮动窗口。手势仿 `PanelResizer` 模式（控件事件 → ICommand → `ShellLayoutState` 转换），新增 `MoveTab` 转换方法。
5. **持久化**：`%AppData%/Digital.Workstation/layout.json`，含 `version` 字段的专用 DTO（不直接序列化 `ShellLayoutState`）；每次布局变更后防抖约 500ms 写盘；启动时贡献装载后恢复。文件缺失/损坏/版本不识别 → 静默回落 attribute 默认布局。配置中找不到对应贡献的条目丢弃，配置缺失的新工具视图落回 `Default` 声明位置。`PanelAlignment` 一并持久化。
6. **视图实例**：同一 `Id` 全应用单实例缓存；跨 Bar 迁移时实例随 tab 走（Avalonia 同一 Control 不能挂两棵视觉树，迁移即激活目标并脱离源显示区）。
7. **重置入口**：视图菜单加「重置布局」项：删除配置文件并按 attribute 默认立即重建布局。

## 后果

- 得：三处 Bar 的条目完全可互换，布局即用户数据；模块侧注册简化为「View 类上加 attribute + 一行 `RegisterToolViews`」；延续 ADR-0001 的 attribute 扫描单一管道，无第二套贡献机制。
- 得：持久化基础设施（DTO + 防抖写盘 + 容错回落）从零建立后，未来其他状态（窗口位置等）可复用同一落盘通道。
- 失：两个贡献接口删除级联全部实现方（shell 5 个 + DashBoard 2 个）与收集器；`ShellLayoutState` 的 `Tabs` 初始化语义从「贡献静态分桶」变为「配置优先、默认兜底」，装载逻辑变复杂。
- 失：Id 冲突、配置与代码漂移等场景走静默规则（丢弃/回落），不报错；只能靠 `Id` 带模块前缀的约定自律。
- 失：ActivityBar 顶部段与 SideBar 的「单内容」语义保留——面板 tab 拖入 ActivityBar 即从「列表成员」变「可切换的独占内容」，两种激活守卫（面板收起拒绝激活 vs 选中展开 SideBar）在统一模型下并存，需文档说清。
