# 设置项以 Attribute 注册，设置页为普通主视图贡献

## 状态
已接受

2026-09-15 修订：分组改按稳定 ID 合并，显示名与枚举选项由贡献者资源解析，替换原全局 `Language` 与名称键兼作分组身份的约束。设置项持久化 ID 与需重启语义不变。

## 上下文

应用需要统一设置机制：任意模块（含 Framework 自身）都能声明设置项，用户在设置页集中浏览与修改。仓库已有两套 attribute 扫描注册机制（菜单 [ADR-0001](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0001-attribute-menu-registration.md)、命令 [ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)）、主视图贡献管线（`IMainViewContribution` + `OpenMainViewEvent`，主区域为 ContentControl 单视图切换）、以及 `LayoutPersistence` 的 JSON 落盘样板。左下角现有 `shell.settings` 钉住工具视图是空壳，仅显示在 SideBar，承载不了分组树 + 编辑器的设置表单。

备选方案：(B) 设置页保留为 SideBar 工具视图——SideBar 宽度装不下分组树 + 编辑器双栏，且设置是低频全局内容，占主区域合理；(C) 强类型设置类（.NET Options 模式，每模块一个 POCO）——读取端类型安全，但引入第二套注册与读取模式，与现有 attribute 贡献体系不同构，持久化逻辑还会耦合进 POCO。

## 决策

1. 设置项经 attribute 声明，拆两个 attribute 仿菜单模型（[ADR-0001](0001-attribute-menu-registration.md)）：`SettingGroupAttribute(id, resourceType, name)` 标类，声明稳定分组 ID、名称资源来源与 Order；`SettingItemAttribute(group, resourceType, name)` 标公共静态可读属性，携带所属分组 ID、名称资源来源、默认值、Order、RequiresRestart。设置项 Id 仍默认「声明类全名.属性名」。模块 `RegisterTypes` 显式调用 `RegisterSettings(Assembly)` 扫描本模块程序集，不做全局程序集扫描。
2. 资源键只在声明的 `ResourceType` 中查找，缺键返回键名，不跨模块查找。同分组 ID 全局合并，保留首份声明的名称键/资源类型，Order 取最小；不同 ID 即使显示名或键相同也不合并。仅被设置项引用的未声明分组仍补出，显示稳定 ID、Order 为 0。分组 ID 不是 settings.json 的 key。
3. 值经 `ISettingsService`（Core/Abstractions 契约，Framework 实现）读写：启动时一次性加载入内存，`Get<T>(id)` 纯内存读，`Set<T>(id, value)` 更新内存、防抖落盘并广播变更事件（EventAggregator，事件契约在 Core/Models）。默认值只是未修改时的取值，不是单独存储层。
4. 持久化为 `%AppData%/Digital.Workstation/settings.json` 单文件，key 为设置项 Id，改即存，读写模式仿 `LayoutPersistence`（System.Text.Json、防抖）。
5. 设置页是新模块 Modules/Settings 贡献的普通 `IMainViewContribution`（左侧分组树 + 右侧编辑器），走既有「收集 → OpenMainViewEvent → MainContent 切换」管线，无特权机制。主视图 Id 常量 `WellKnownViews.Settings` 放 Core/Abstractions，与 `ShellRegions` 常量同处——shell 按钮与 Settings 模块都依赖它，而 Workstation 不引用 Settings 模块。
6. 左下角按钮改为纯导航：点击发布 `OpenMainViewEvent` 打开设置页；`shell.settings` 钉住工具视图（空壳）删除。
7. RequiresRestart 设置项：修改后值立即落盘，当前进程行为不变，下次启动由消费方读取生效。UX：该项旁显示「重启后生效」项级标记；存在未生效修改时页面顶部出横幅，带「立即重启」按钮（启动新进程后退出当前进程）。
8. 编辑器控件由设置项声明类型推断，当前枚举映射为下拉框。枚举成员键仍按「设置项名称键 + 成员名」派生，但查找限定于该设置项的资源类型；缺键显示派生键名，与 `ResourceText.Get` 一致。
9. 首批设置项为 Framework 的「常规 / 语言」（zh-CN / en-US，enum，RequiresRestart），分组 ID 为 `GeneralSettings.GroupId`（`framework.general`），名称和选项使用 `FrameworkResources`。Settings 模块不加演示项，自己的重启提示使用 `SettingsResources`，不接管贡献者的名称资源。

## 后果

- 得：第三套 attribute 注册与前两套同构，模块侧注册 = attribute 标注 + 一行 `RegisterSettings`；设置页复用主视图管线，零特殊路径；空壳工具视图消除；设置机制对 Framework 与业务模块一视同仁。
- 失：读取端是字符串 Id + 泛型 `Get<T>`，无编译期类型安全，Id 冲突与拼写错误只能靠文档约定（建议模块名前缀）与运行时日志自律——与 [ADR-0001](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0001-attribute-menu-registration.md) 的静默冲突规则同型。
- 失：`ISettingsService` 进 Core/Abstractions 是最底层契约的新成员，其签名改动将级联全部消费方。
- 本地化修订：模块可以携带自己的设置项翻译，Settings 页面无需知道有哪些模块资源；代价是元数据增加资源类型，分组稳定 ID 需显式命名。语言选择仍由宿主在构造界面及收集标题前统一应用，不引入运行时热切换。
