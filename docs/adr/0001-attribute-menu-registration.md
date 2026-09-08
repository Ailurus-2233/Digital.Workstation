# 菜单贡献改为路径/分组模型并以 Attribute 注册

## 状态
已接受

## 上下文

旧菜单贡献契约 `IMenuItemContribution` 用封闭枚举 `MenuPlacement { File, View, Help }` 定位，菜单栏是 `MainWindow.axaml` 里写死的三个顶层菜单，贡献项只能平铺填入，无法表达多级子菜单、分组与自动分隔线（视图菜单的对齐组/显隐组分隔线是 shell 侧特判插入的）。新需求要求任意深度菜单路径（`File/Export`、`Tools/Diagnostics/Performance`）、命名分组（Group/GroupOrder）与组间自动分隔线，并要求以 Attribute 声明、启动时反射扫描一次注册。

备选方案：(B) 保留旧契约不动，新建并行的 attribute 菜单体系，shell 合并两个来源；(C) attribute 生成旧契约适配器。C 走不通——旧契约装不下分组与路径；B 让两套排序语义在同一菜单里相遇，规则需要额外发明，且双机制是永久的理解负担。

## 决策

1. 演进 `IMenuItemContribution` 本身：`Menu` 枚举改为 `Path` 字符串，新增 `Group`/`GroupOrder`；接受对全部实现方与收集方的破坏性级联。
2. Attribute（`MenuGroup` 标类、`MenuItem` 标方法）只是生成该契约实现的一种新注册方式；shell 保持单条「收集 → 建树 → 渲染」管道。
3. 各模块在 `RegisterTypes` 显式调用 `RegisterMenus(Assembly)` 扫描本模块程序集，把菜单类注册进容器；shell 在既有的一次性收集点（`EnsureContributionsLoaded`）从容器解析并建树。不做全局程序集扫描。
4. 顶层菜单的 Order 由指向首段路径的 `MenuGroup.Order` 声明；多处声明冲突取最小值。同名 Group 的 GroupOrder 冲突同样取最小值。`MenuGroup.Order` 未声明时视为"无位次意见"（缺省 `int.MaxValue`，排最后），不参与取最小——否则任何忘写 Order 的类会以缺省 0 把所在菜单钉到最前。
5. Attribute 中的标题字符串是 `Language` 资源键，运行时解析，缺键回退键名本身（resx 既有行为）。
6. 既有 7 个菜单贡献实现（含参数化的 `TogglePanelContribution`/`PanelAlignmentContribution` 工厂循环）全部迁移为 attribute 菜单类；视图菜单的 shell 分隔线特判随之删除，由分组自然表达。
7. 方法签名仅支持 `void M()` 与 `Task M()`；非法签名扫描时记日志跳过。`Task` 执行异常记日志不抛出。本期不做 CanExecute/禁用态与快捷键。
8. 菜单图标保留：`MenuItem` attribute 带可选 `Icon` 属性（`Icons.Xxx` 的 path 字符串），契约的 `IconPath` 改为可空，为空时模板不渲染图标。预置项迁移后图标原样保留，无 UI 回退。
9. 菜单契约移除 `Id` 属性：菜单链路无任何消费方（导航项/面板 tab 的 Id 才参与行为），YAGNI。菜单项不再有稳定标识，定位完全由路径 + 分组 + 位次表达。
10. 多段路径语义：`MenuGroup` 的 `Group`/`GroupOrder`/`Order` 描述该类在父菜单里**直接贡献的东西**——单段路径时是方法项的分组；多段路径时是末端子菜单节点在其父菜单内的分组与位次，此时方法项进入末端菜单的默认组。一个 attribute 只有一套分组参数；本期不支持在深层子菜单内部再分组（方法项一律进默认组）。
11. 默认组：`Group` 未指定的条目归入无名默认组，`GroupOrder` 视为 `0` 排最前；`GroupOrder` 本身缺省值亦为 `0`。
12. 菜单类以 singleton 注册进容器，`EnsureContributionsLoaded` 建树时解析一次，方法委托缓存进生成的贡献对象；实例寿命 = 应用寿命，不重复扫描、不重复解析。
13. 路径归一化：各段 `Trim` 后按序精确匹配（Ordinal，大小写敏感）——路径段同时是定位标识和 `Language` 键，键的大小写敏感性与 resx 查找一致。含空段（`"File//Export"`）的路径整体非法，该类全部条目记日志跳过。
14. 平局规则：同 Group 同 Order 的条目按解析后的 Title 字典序（Ordinal）排序，与模块加载顺序无关，界面上可解释。

## 后果

- 得：菜单系统获得任意深度路径与分组能力；模块侧注册从"手写契约实现类"简化为"attribute 标注的普通类 + 一行 RegisterMenus"；shell 的分隔线特判逻辑消除；保持单一渲染管道，无双机制并存。
- 得：菜单类经容器解析，构造注入（`IEventAggregator`/`IWindowManager`）原样可用。
- 失：`MenuPlacement` 枚举删除级联所有实现方（7 个）与收集方；菜单栏从静态 XAML 改为动态生成，`MainWindow.axaml` chrome 与 `MainWindowViewModel` 的三个菜单集合需要重写。
- 失：冲突取最小值是静默规则，模块间分组位次写错时不报错，只能靠文档约定（建议模块名前缀）自律。
