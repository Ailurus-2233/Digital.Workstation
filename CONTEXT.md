# Digital.Workstation

模块化 Avalonia 桌面工作站：自定义引导加载的 Launcher 启动 Prism 应用，业务功能以 Prism 模块形式接入。

## Language

### 启动与加载

**Launcher（启动器）**:
WinExe 进程入口。先引导自定义的程序集与原生库加载，再把控制权交给应用本体。
_Avoid_: Bootstrapper、引导程序

**AssemblyLoader（程序集加载器）**:
自定义加载器，负责程序集的按需解析与原生库的 P/Invoke 兜底。Release 下启用，Debug 下整体短路。
_Avoid_: 加载器（单独使用时指代不清）

**引导程序集（Boot Assemblies）**:
启动最早阶段必须先行加载的一小组程序集（日志、核心基础设施、UI 框架），其余程序集按需解析。
_Avoid_: 预加载 DLL

**搜索路径（Search Paths）**:
输出目录中供按需解析的一组分类目录（core、libraries、modules、runtimes），各有固定递归深度。

**输出布局（Output Layout）**:
Release 构建产物按角色分目录归置的结构（core/libraries/modules/runtimes），是自定义加载的前提；Debug 构建平铺输出、不套用此布局。

### 应用与窗口

**Workstation（主应用）**:
Avalonia 应用本体与主窗口所在处，负责装配 Prism 模块目录。不是 Module，尽管其编译产物归置于 modules/ 目录。
_Avoid_: 模块（指代它时）

**主窗口（MainWindow）**:
应用的主窗口。启动时只登记、不显示，由事件触发后才成为可见主窗口。
_Avoid_: Shell

**模块（Module）**:
Prism `IModule`——编译期进入依赖图、运行时被模块目录激活的功能单元。专指 Prism 模块。
_Avoid_: 用 "Module" 指代 Modules/ 目录下的任意项目（Workstation 不是 Module）

**DashBoard（启动台）**:
启动后首先可见的独立窗口，显示核心模块装载的流程进度，完成后进入主窗口。真实产品概念，非临时脚手架。
_Avoid_: 示例模块（README 的旧称，已过时）

### Shell 布局区域

**ActivityBar（活动栏）**:
工作区最左侧的竖向导航栏，由顶部 tab 项与底部工具栏构成；导航项由模块贡献。

**SideBar（侧边栏）**:
ActivityBar 右侧的容器，显示当前选中导航项的内容。可折叠、可调宽。

**MainContent（主内容区）**:
工作区中央的主 Region，单视图切换（无文档 tabs）：同时只有一个活动视图，由 SideBar 内的交互驱动切换，不与 ActivityBar 导航项强制联动。默认显示 shell 内置的空状态页（快捷键/常用命令提示）。

**AuxiliaryPanel（辅助面板）**:
工作区右侧的 tab + 容器区域，承载辅助信息。可折叠、可调宽。

**BottomPanel（底部面板）**:
工作区底部的 tab + 容器区域，承载输出/日志类内容。可折叠、可调高。

**贡献（Contribution）**:
模块向 shell 声明可组合元素的机制：模块实现契约接口（导航项、面板 tab、菜单项、工具栏项、状态栏项），shell 收集并按元数据（图标、标题、排序、Placement）渲染。视图本体由 Prism Region 托管。
_Avoid_: 模块直接操作 shell 的 UI 元素

**窗口管理器（WindowManager）**:
窗口显示/隐藏/关闭/对话框的统一契约（`IWindowManager`/`IMainWindowManager`）。模块经它操作窗口，不直接实例化窗口。

### 横切

**主题包（UIPackage）**:
把多个第三方主题（Semi、Ursa 等）聚合为单一应用主题的资源包。
_Avoid_: 主题集合、样式包
**资源包（Resource）**:
Core/Resource，界面文案的集中管理：语言资源文件（中性 resx 为中文，en-US 为英文卫星程序集）+ `Language` 静态访问类（`Get(key)` 通用取值 + 各文案的强类型属性）。C# 中的显示字符串（标题、菜单文本等）一律经 `Language` 获取，不在类中硬编码；Id 类标识符（如导航项 Id）不是文案，仍直接定义在类中。
_Avoid_: 在 C# 类中直接写字面量文案

**共享图标（Icons）**:
Core/UIPackage 的 `Icons` 静态类，集中提供 StreamGeometry path 字符串图标（PathIcon 消费、随主题变色）。贡献类经 `Icons.X` 引用图标，不在各自类中内联 path 字符串。
_Avoid_: 在贡献类中硬编码图标 path

**共享事件（Shared Events）**:
模块间通信的事件契约（Prism PubSubEvent），集中定义在 Core/Models，如 `ShowMainWindowEvent`。
_Avoid_: 把事件定义在任一业务模块内部

**UI 测试（人工验收）**:
UI 不写自动化测试——UI 相关的开发不需要创建单元测试，也不维护 UIA 冒烟脚本；UI 行为由人工验收排查，人工验收通过后即可提交。单元测试仅用于与 UI 无关的逻辑。
_Avoid_: 为 UI 行为编写 xUnit/UIA 等自动化测试
