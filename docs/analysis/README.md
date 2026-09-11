# Digital.Workstation — 模块深读文档索引

本目录是对 Digital.Workstation 桌面应用全部源码模块的系统性深读产物，面向**没读过源码的 agent 与人**：先按模块索引定位模块，进模块目录先读 `common.md`，再按场景指南跨模块串读。

- 仓库来源：本地仓库 `D:/Sources/Person/Digital.Workstation`
- 版本：commit `04cfd02`（`04cfd0275aa418ed7061cd26037a450f49700bac`）
- 跟踪分支：`main`
- 生成时间：2026-09-06

## 每个模块目录的结构

9 个模块目录各自包含同一组 8 个文件（以 `Core/Abstractions/` 为例，标题即文件用途）：

| 文件 | 内容 |
|---|---|
| `common.md` | **入口**：模块做什么、核心设计逻辑、状态流转、常见修改场景 |
| `reference.md` | 模块关系链：csproj 依赖/被依赖清单（含行号）、核心内部数据结构 |
| `api.md` | 对外接口与调用方式（类型/成员表，带源码路径行号） |
| `glossary.md` | 术语表：领域术语与缩写，标注首次出现位置 |
| `pitfalls.md` | 不变量与陷阱：编译期不强制、改错才暴露的约定 |
| `error.md` | 异常与排查：每类错误的触发条件、行为、排查步骤 |
| `testing.md` | 验证方式：测试项目、框架、怎么跑、覆盖边界 |
| `file-list.md` | 文件结构与功能：目录树逐文件一句话职责 |

**阅读规则：进任何模块先读 `common.md`**；要改代码再读同目录 `pitfalls.md` 与 `api.md`；要理解跨模块关系读 `reference.md`。

## 模块索引

依赖列为该项目 `.csproj` 中 `ProjectReference` 的直接项目依赖（权威来源，见下节）；各模块职责提炼自各自 `common.md`。

| 模块 | 文档目录 | 一句话职责 | 依赖 |
|---|---|---|---|
| Core/Abstractions | [Core/Abstractions/](Core/Abstractions/common.md) | 纯契约层：贡献接口与定位枚举（`Contributions/`）、菜单契约 `IMenuItemContribution` 与 `MenuGroupAttribute`/`MenuItemAttribute`（`Menus/`）、命令契约 `ICommandContribution` 与 `CommandAttribute`（`Commands/`，ADR-0005）、设置契约 `SettingGroupAttribute`/`SettingItemAttribute`/`ISettingsService`（`Settings/`，ADR-0006）、`ShellRegions` 常量（`Regions/`）、窗口管理接口（`WindowManager/`），零实现 | 无项目依赖（包：Avalonia） |
| Core/Common | [Core/Common/](Core/Common/common.md) | 基础设施静态门面：Serilog 静态日志 `Logger` 与 Prism 容器静态访问器 `IoC`，进程内单例 | Abstractions（包：Prism.Avalonia/DryIoc、Serilog） |
| Core/Models | [Core/Models/](Core/Models/common.md) | 跨模块事件契约与负载 DTO 层：启动序列三件套 + 工作区交互三件套 + 设置变更事件 `SettingChangedEvent`（ADR-0006 决策 3，共 7 个事件），全是空 `PubSubEvent<T>` 子类与 record/枚举 | Common |
| Core/Resource | [Core/Resource/](Core/Resource/common.md) | UI 文案资源层：静态类 `Language` + 中文中性 `Language.resx` / 英文 `Language.en-US.resx`，键缺失返回键名本身 | 无项目依赖 |
| Core/UIPackage | [Core/UIPackage/](Core/UIPackage/common.md) | 共享 UI 资源包：`WorkstationTheme` 聚合 4 个第三方主题包、`VSCodePalette` 深色色键、`Icons` 15 个 StreamGeometry path 常量 | 无项目依赖（包：Avalonia/Semi.Avalonia/Ursa） |
| Core/Framework | [Core/Framework/](Core/Framework/common.md) | 应用框架层：`FrameworkApplication<TWindow>` 引导与三阶段启动序列（ADR-0004）、`FrameworkWindow` 主题窗口基类（`Windows/`）、`CommandPalette` 命令面板控件（ADR-0005）、`FrameworkWindowManager`（`WindowManager/`）、`ShellLayoutState` 布局状态机与 `LayoutPersistence` 布局持久化（`Layout/`）、`ShellContributionCollector` 贡献收集（`Contributions/`）、`MenuTreeBuilder` 菜单建树与 `RegisterMenus`/`RegisterCommands` attribute 菜单/命令注册（`Menus/`、`Commands/`）、`RegisterSettings` attribute 设置注册与 `SettingsService` 设置持久化/语言应用（`Settings/`，ADR-0006） | Abstractions、Common、Models、UIPackage |
| Modules/DashBoard | [Modules/DashBoard/](Modules/DashBoard/common.md) | 启动台模块：启动进度窗（进度/失败/继续退出决策）+ 向 shell 五个扩展点各贡献一条目的通路验证（tracer bullet） | Abstractions、Framework、Resource、UIPackage |
| Modules/Settings | 尚无，待 deep-read 生成 | 设置页模块：向 MainContent 贡献设置页主视图（分组树 + 编辑器，含「重启后生效」标记与重启横幅 UX，ADR-0006 决策 5/7） | Abstractions、Framework、Resource、UIPackage |
| Modules/Workstation | [Modules/Workstation/](Modules/Workstation/common.md) | 应用宿主与 shell：`MainWindow` VS Code 式五区布局、`MainWindowViewModel` 驱动布局状态（含命令收集与手势 KeyBinding 接线，ADR-0005；ActivityBar 左下角"设置"纯导航按钮，ADR-0006 决策 6）、`WorkstationApplication` 入口、shell 预置贡献 | Framework、Resource、UIPackage、DashBoard、Settings |
| Launcher | [Launcher/](Launcher/common.md) | 程序入口与运行时引导器（WinExe）：Release 分类目录布局的程序集/native 库解析（`AssemblyLoader`）+ 启动 Avalonia/Prism 应用 | Workstation |

另有 `UnitTest/Framework`（xUnit 测试项目，仅测 `ShellLayoutState`，引用 Framework），不是被索引模块；其内容见 [Core/Framework/testing.md](Core/Framework/testing.md)。

## 依赖关系图

权威来源：各 `.csproj` 的 `ProjectReference`（各模块 `reference.md` 的「依赖关系」节逐条记录了行号与用途）。

```mermaid
graph TD
  Launcher["Launcher (WinExe 入口)"] --> WS["Modules/Workstation"]
  WS --> FW["Core/Framework"]
  WS --> Res["Core/Resource"]
  WS --> UIP["Core/UIPackage"]
  WS --> DB["Modules/DashBoard"]
  WS --> ST["Modules/Settings"]
  DB --> Abs["Core/Abstractions"]
  DB --> FW
  DB --> Res
  DB --> UIP
  ST --> Abs
  ST --> FW
  ST --> Res
  ST --> UIP
  FW --> Abs
  FW --> Com["Core/Common"]
  FW --> Mod["Core/Models"]
  FW --> UIP
  Mod --> Com
  Com --> Abs
```

要点（细节见各 `reference.md`）：

- **业务模块不引用 shell**：DashBoard 只引用 Core 层项目，对 shell 的集成全靠实现 Abstractions 的贡献接口 + Models 的事件（`Modules/DashBoard/common.md`「依赖面收窄到 Core」）。
- **Models 经 Framework 传递到达消费方**：DashBoard/Settings/Workstation 不直接引用 Models，经 `Framework → Models` 传递引用获得事件类型（`Core/Models/reference.md` 被依赖关系表）。
- **Resource/UIPackage 是 Core 层最底**：无任何项目依赖，被 Framework 与三个 Modules 项目直接引用。
- 包依赖（非项目依赖）：Abstractions 仅 Avalonia；Common 为 Prism.Avalonia/Prism.DryIoc.Avalonia/Serilog；UIPackage 为 Avalonia/Semi.Avalonia 系列/Irihi.Ursa.Themes.Semi。

## 跨模块场景指南

每个场景给出涉及模块的文档路径与建议阅读顺序；场景内论断均可沿所引路径核实。

### 1. 新增一个业务模块并向 shell 贡献导航项/面板 tab/菜单项

1. [Core/Abstractions/common.md](Core/Abstractions/common.md) + [api.md](Core/Abstractions/api.md)：5 个 `I*Contribution` 接口的字段骨架（导航/主视图/面板/状态栏为 `Id`/`Title`/`IconPath`/`Order` + 定位枚举；`IMenuItemContribution` 为 ADR-0001 路径/分组模型，通常不经手写实现而由 `MenuGroupAttribute`/`MenuItemAttribute` + `RegisterMenus` 声明注册）。
2. [Core/Abstractions/pitfalls.md](Core/Abstractions/pitfalls.md)：`Id` 唯一性分级（主视图 Id 跨模块全局唯一，建议模块名前缀）与 `Order`「小者靠前」的作用域。
3. [Modules/DashBoard/common.md](Modules/DashBoard/common.md) + [reference.md](Modules/DashBoard/reference.md)：现成模板——DashBoard 是工具视图/主视图/状态栏三类扩展点各贡献一条的 tracer bullet，`DashBoardModule.RegisterTypes` 是注册样板（工具视图一行 `RegisterToolViews(typeof(...).Assembly)`，接口贡献逐行 `RegisterSingleton`），贡献类属性矩阵在 reference.md。
4. [Core/Resource/common.md](Core/Resource/common.md)：贡献项 `Title` 文案的来源（见场景 2）。
5. [Core/UIPackage/common.md](Core/UIPackage/common.md)：贡献项 `IconPath` 的来源（`Icons.Xxx` 常量）。
6. [Modules/Workstation/common.md](Modules/Workstation/common.md)：shell 侧如何收集（`EnsureContributionsLoaded` → `ShellContributionCollector`）并渲染；`WorkstationApplication.ConfigureModuleCatalog` 里 `AddModule<新模块>()`。
7. [Core/Framework/common.md](Core/Framework/common.md)：`ShellContributionCollector` 的过滤/排序语义。

### 2. 新增一条多语言文案

1. [Core/Resource/common.md](Core/Resource/common.md)「常见修改场景 1」：三处必须同步——`Core/Resource/Language.resx`（中文）、`Language.en-US.resx`（英文）、`Language.cs`（`nameof` 属性）；漏加 en-US 不报错，静默回退中文。
2. [Core/Resource/pitfalls.md](Core/Resource/pitfalls.md)：键与属性同名不变量、resx `data name` 必须手动同步重命名。
3. 消费点定位：[Modules/Workstation/reference.md](Modules/Workstation/reference.md) 与 [Modules/DashBoard/reference.md](Modules/DashBoard/reference.md) 的「依赖关系」表列出了全部 `Language.*` 使用点（带行号）。
4. 排查界面上出现英文键名：见 [Core/Resource/common.md](Core/Resource/common.md) 场景 5。

### 3. 启动失败排查链

1. [Launcher/error.md](Launcher/error.md)：分水岭判据——看不到 `"Application startup."` 日志 = 崩在 `AssemblyLoader` 引导阶段；看到后才崩 = Avalonia 启动后的问题。Release 分类目录布局与 `Build/ManageDlls.targets` 的对偶关系见 [Launcher/reference.md](Launcher/reference.md)。
2. [Core/Common/common.md](Core/Common/common.md)：日志只有 Console sink，GUI 子系统无附加控制台时不可见——先确认日志是否可达。
3. [Core/Framework/common.md](Core/Framework/common.md)「状态流转」：三阶段启动序列（CoreServices → LoadingModules → Ready）逐步骤，含模块加载失败的捕获与决策回传（`RunStartupSequenceAsync`）。
4. [Core/Models/common.md](Core/Models/common.md)：启动事件三件套的负载语义（`StartupProgress`/`ModuleLoadFailure`/`StartupFailureAction`）。
5. [Modules/DashBoard/common.md](Modules/DashBoard/common.md)「启动台链」：进度/失败在启动台 UI 上的呈现与「继续/退出」回传路径。

### 4. 新增一条跨模块事件契约并发布/订阅

1. [Core/Models/common.md](Core/Models/common.md)：事件定义形态——空 `PubSubEvent<T>` 子类 + record 负载 + 「谁发布、谁订阅」XML 注释；`EventAggregator.GetEvent<T>()` 按类型身份撮合，事件类型只能有唯一定义点（本模块）。
2. [Core/Models/reference.md](Core/Models/reference.md)：现有 6 个事件的负载结构与字符串级契约（如 `OpenMainViewEvent` 负载 = `IMainViewContribution.Id`，不匹配则静默无反应）。
3. 发布/订阅样例：[Core/Framework/common.md](Core/Framework/common.md)（启动序列发布端）、[Modules/DashBoard/common.md](Modules/DashBoard/common.md)（启动台订阅/发布端）、[Modules/Workstation/common.md](Modules/Workstation/common.md)（`MainWindowViewModel` 订阅端）。
4. 接线：消费方经 `Framework → Models` 传递引用即可解析事件类型，通常无需新增 ProjectReference（[Core/Models/common.md](Core/Models/common.md) 场景 3）。

### 5. 改主题配色 / 新增共享图标

1. [Core/UIPackage/common.md](Core/UIPackage/common.md)：调色板键在 `VSCodePalette.ApplyTo`（写进应用级资源以覆盖主题包同名键），图标在 `Icons.cs` 的 const。
2. [Core/UIPackage/pitfalls.md](Core/UIPackage/pitfalls.md)：`Icons` 的 const 会被内联进引用程序集——改值后须全量重编译消费方。
3. [Core/Framework/common.md](Core/Framework/common.md)：`FrameworkApplication.Initialize` 是全应用唯一主题装载点（固定 `ThemeVariant.Dark` + `Styles.AddRange(new WorkstationTheme())` + `VSCodePalette.ApplyTo(Resources)`）。
4. [Modules/Workstation/common.md](Modules/Workstation/common.md)：消费侧全部经 `MainWindow.axaml` 的 `{DynamicResource ...}` 键查找，改色不需动控件。

### 6. 新增一种 shell 贡献类型（新扩展点）

1. [Core/Abstractions/common.md](Core/Abstractions/common.md) 场景 1：新建 `IXxxContribution.cs`（放 `Abstractions/Contributions/`），仿现有 `I*Contribution` 字段结构（`Id`/`Title`/`IconPath`/`Order` + 定位枚举；注意 `IMenuItemContribution` 已是路径/分组模型并配 attribute 注册，不宜作通用模板）；如需新 Region 在 `ShellRegions.cs`（`Abstractions/Regions/`）加常量。**这是破坏性变更**——最底层契约的签名改动会级联全部实现方与收集方。
2. [Core/Framework/common.md](Core/Framework/common.md) 场景 3：`ShellContributionCollector` 加一个 `Get*` 收集方法（Resolve → Where → OrderBy → ToArray 模式）。
3. [Modules/Workstation/common.md](Modules/Workstation/common.md)：shell 侧消费（`MainWindowViewModel` 的集合、缓存字典、XAML 呈现点）。
4. [Modules/DashBoard/common.md](Modules/DashBoard/common.md)：模块侧第一个实现样例（含 `RegisterTypes` 注册行）。

### 7. 新增一个命令（出现在命令面板）

1. [Core/Abstractions/common.md](Core/Abstractions/common.md) + [api.md](Core/Abstractions/api.md)：`ICommandContribution`/`CommandAttribute` 字段骨架（`Id` 默认「声明类全名.方法名」、`Title` 资源键、`Gesture`、`Order`；扁平模型，与菜单互不相干，ADR-0005）。
2. 模块侧：在任何类的方法上标 `[Command("标题键", Order=…, Gesture=…)]`（免类级 attribute，仅支持无参 `void`/`Task`），模块 `RegisterTypes` 保证调了 `RegisterCommands(Assembly)`——现成样例 [Modules/Workstation/](Modules/Workstation/common.md) 的 `ViewCommands`；标题键同步在 Core/Resource 加（或复用既有键）。
3. [Core/Framework/common.md](Core/Framework/common.md)：收集侧零改动——`ShellContributionCollector.GetCommands()` 统一排序去重；`CommandPalette`（Ctrl+P）与 `RegisterCommandGestures` 接线已在 shell 就位。

### 8. 新增一个设置项（出现在设置页）

1. [Core/Abstractions/common.md](Core/Abstractions/common.md) + [api.md](Core/Abstractions/api.md)：`SettingGroupAttribute`（类级、可多处声明同名分组取最小 `Order`）/`SettingItemAttribute`（标公共静态可读属性作声明锚点，`Id` 默认「声明类全名.属性名」、`DefaultValue`、`Order`、`RequiresRestart`）与 `ISettingsService`（`Get<T>`/`Set<T>`/`IsPendingRestart`，ADR-0006）字段骨架；`Get` 对未修改值回退声明默认值，资源键缺失时界面显示键名本身（`Language.Get` 的 `?? key` 兜底）。
2. 模块侧：在静态类上标 `[SettingGroup("分组名键", Order=…)]`、在静态可读属性上标 `[SettingItem("分组名键", "设置项名键", DefaultValue=…)]`，模块 `RegisterTypes` 保证调了 `RegisterSettings(Assembly)`；分组名/设置项名/枚举成员显示名（「设置项名称键 + 成员名」约定）三类键同步在 Core/Resource 加（见场景 2）——现成样例为 Framework 预置的 `GeneralSettings`。
3. [Core/Framework/common.md](Core/Framework/common.md)：收集侧零改动——`ShellContributionCollector.GetSettingGroups()`/`GetSettingItems()` 统一合并排序去重；代码读写设置经注入 `ISettingsService`（`Set` 自动防抖落盘并广播 `SettingChangedEvent`），不读 attribute 属性值。
