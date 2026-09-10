# 命令体系：Attribute 注册、全局命令列表与命令面板

## 状态
已接受

## 上下文

应用需要一个类似 VS Code Quick Open 的命令面板：主窗口按 Ctrl+P 在顶部弹出命令框，列出全部命令，支持检索与执行。命令的注册方式要求与菜单（ADR-0001）类似——attribute 标方法、模块加载时扫描、汇入全局命令列表。现有事实：菜单管线已成型（`MenuGroup`/`MenuItem` attribute + `RegisterMenus` 扫描 + 容器收集），但菜单契约刻意没有 Id（ADR-0001 决策 9）；全仓无全局快捷键框架，仅 `MainWindow.axaml` 三条硬编码 KeyBinding（Ctrl+B/Ctrl+J/Ctrl+Alt+B），与菜单 attribute 无关联。

备选方案：(B) 命令复用菜单体系，菜单项自动全部进命令面板——菜单项无稳定 Id，无法支撑最近使用（MRU）与键绑定引用，且「哪些菜单项进面板」需要额外发明规则；(C) 面板交互状态（过滤文本、选中项、MRU）放进 shell ViewModel——`MainWindowViewModel` 已承载全部布局状态，面板状态是纯瞬时 UI 状态，放进去只会继续撑大它；(D) MRU 持久化到 %AppData%——引入文件格式与版本问题，收益不成比例，留作后续。

## 决策

1. 命令是独立于菜单的新贡献类型：`CommandAttribute`（标方法）+ `ICommandContribution` 契约（Abstractions 新增 `Commands/` 目录）+ Framework 侧 `RegisterCommands(Assembly)` 扫描扩展，管线与菜单同构（注册期扫描一次 → 容器 → shell 一次性收集）但互不相干。菜单项不进命令面板。
2. 免类级 attribute：程序集内任何类的公共实例方法标 `[Command]` 即被扫描，类仅作 DI 宿主（命令没有路径/分组概念，类级标记是纯仪式）。
3. 命令 Id：attribute 未显式指定时默认 `声明类全名.方法名` 生成，可命名属性覆盖；Id 冲突时后注册者丢弃并 `Logger.Warning`。Id 的用途是 MRU 记忆与未来的键绑定引用。
4. 执行模型与菜单完全同构：仅支持无参 `void M()`/`Task M()`，非法签名扫描时记日志跳过；命令类注册 singleton 经容器解析，构造函数注入可用；`DelegateCommand` 包装反射调用，`Task` 异常记日志不抛出。本期不做 CanExecute/禁用态与参数化命令。
5. `CommandAttribute` 带 `Gesture` 命名属性（如 `"Ctrl+Shift+P"`）：声明式快捷键。shell 收集命令后为带 Gesture 的命令生成窗口级 KeyBinding（机制在 Framework，接线在 shell 模块，同 `LayoutPersistence` 的分层惯例）。现有三条硬编码面板快捷键本期不迁移。
6. 面板 UI 是 Framework 的自包含控件 `CommandPalette`：搜索框 + 列表 + 子串过滤（不区分大小写，匹配本地化后标题）+ ↑↓/Enter/Esc 键盘导航 + MRU 内存分组置顶，全部内聚在控件内；控件寿命 = 窗口寿命 = 应用寿命，内存 MRU 因此成立。`FrameworkWindow` 构造时代码创建面板并注册 Ctrl+P KeyBinding 直接开关，不动四份静态布局模板；ViewModel 契约只新增一个 `Commands` 集合（宽松绑定，同 `MenuBarItems` 惯例）。
7. 标题在收集时经 `Language.Get` 一次解析（与菜单建树一致）；列表项只显示标题 + 右侧快捷键文本，无图标；输入框无 `>`/`#` 前缀模式语法；无匹配时显示空态文案；Esc、失焦、执行命令后关闭面板。
8. MRU 只在内存中，不持久化；持久化留作后续工单（可仿 `LayoutPersistence` 模式）。

## 后果

- 得：命令获得与菜单同等纪律的声明式注册通路；声明式快捷键终结「快捷键与菜单 attribute 无关联」的割裂；面板逻辑自包含，shell ViewModel 零交互逻辑增量。
- 得：`FrameworkWindow` 内置使所有子类窗口一致获得 Ctrl+P 与命令快捷键能力。
- 失：菜单项与命令两套声明并存，同一动作若想同时出现在菜单栏与命令面板需要两处标注（刻意取舍，见备选 B）。
- 失：MRU 重启即清；快捷键冲突（两个命令声明同一 Gesture）只能后注册者覆盖或记日志，无冲突解决 UI。
