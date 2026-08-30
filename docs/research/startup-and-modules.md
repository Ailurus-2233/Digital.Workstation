# Digital.Workstation 启动链路与模块职责研究

> 本文档仅依据仓库内 primary sources（源码、`.csproj`/`.props`/`.targets`、`slnx`、`global.json`、`README.md`）撰写。
> 引用格式为 `相对路径:行号`；标注 **[推断]** 的内容无法从源码直接确定，仅为基于代码行为的合理解读。
> 仓库基线：`net10.0`（`global.json:3`、各 csproj `TargetFramework`），Avalonia 11.3.20 + Prism.Avalonia 9.0。

---

## 1. 总体结构

| 项目 | 程序集名 | 角色 |
|---|---|---|
| `Launcher/Launcher.csproj` | `Launcher`（WinExe） | 进程入口、程序集/原生库加载引导（`Launcher.csproj:4,8`） |
| `Core/Abstractions` | `DigitalWorkstation.Core.Abstractions` | 抽象接口（窗口管理） |
| `Core/Common` | `DigitalWorkstation.Core.Common` | IoC 桥接、Logger |
| `Core/Framework` | `DigitalWorkstation.Core.Framework` | Prism 应用基类、窗口管理器实现 |
| `Core/Models` | `DigitalWorkstation.Core.Models` | 跨模块事件 |
| `Core/UIPackage` | `DigitalWorkstation.Core.UIPackage` | 主题资源包 |
| `Modules/Workstation` | `DigitalWorkstation.Workstation` | 主应用（Application + MainWindow） |
| `Modules/DashBoard` | `DigitalWorkstation.DashBoard` | 示例模块（Prism IModule） |

程序集名规则由 `Build/Base.props:34-47` 统一生成：非 Core 目录下为 `DigitalWorkstation.<项目名>`（`Base.props:34-37`），`Core/` 下为 `DigitalWorkstation.Core.<项目名>`（`Base.props:39-42`）。`Launcher` 因显式设置 `<AssemblyName>Launcher</AssemblyName>`（`Launcher.csproj:8`）不套用该规则。

---

## 2. 启动链路（按执行顺序）

### 步骤 0：进程入口

`Launcher/Program.cs:13-17` — `Main(string[] args)` 只做两件事：

```csharp
Launcher.Initialize();
Launcher.Run(args);
```

`Main` 标记为 WinExe 入口（`Launcher.csproj:4`，`OutputType=WinExe`）。`Program.BuildAvaloniaApp()`（`Program.cs:23`）仅服务 Avalonia 设计器/预览器——设计态下 `AssemblyLoader.Initialize()` 自动禁用（见步骤 2），程序集直接从输出根目录探测（`Program.cs:19-22` 注释）。

### 步骤 1：Launcher.Initialize() —— 加载器初始化

`Launcher/Launcher.cs:21-30`：

1. `AssemblyLoader.Initialize()`（`Launcher.cs:23`）
2. `AssemblyLoader.RegisterNativeResolvers()`（`Launcher.cs:27`）
3. `AssemblyLoader.PreloadNativeLibraries()`（`Launcher.cs:28`）
4. `InitializeCore()`（`Launcher.cs:29`）——当前为空 TODO（`Launcher.cs:32-35`）

### 步骤 2：AssemblyLoader.Initialize() —— 程序集发现与预加载

`Launcher/AssemblyLoader.cs:62-85`，全程持锁、幂等（`AssemblyLoader.cs:64-65`）：

- **设计时短路**：`IsDesignEnvironment()` 在 `#if DEBUG` 下恒返回 `true`（`AssemblyLoader.cs:138-143`），此时直接置 `_isInitialized` 返回，不做任何事（`AssemblyLoader.cs:68-72`）。含义：**所有 Debug 构建（包括运行时调试）都跳过自定义解析流程**，依赖 .NET 默认 probing（与 README 描述一致，`README.md:41`）。
- **step 1 预加载关键 DLL**：`PreloadBootAssemblies(BootRequiredAssemblyFiles)`（`AssemblyLoader.cs:75-76`）。`BootRequiredAssemblyFiles` 列出的 6 个程序集（`AssemblyLoader.cs:23-31`）：
  - `Libraries/Serilog/Serilog.dll`、`Libraries/Serilog/Serilog.Sinks.Console.dll`
  - `Core/DigitalWorkstation.Core.Common.dll`
  - `Libraries/Avalonia/Avalonia.Base.dll`、`Libraries/Avalonia/Avalonia.Controls.dll`、`Libraries/Avalonia/Avalonia.dll`
  
  路径均相对 `AppContext.BaseDirectory`（`AssemblyLoader.cs:18`，即 `Output/Release/`）。- 预加载用 `Assembly.LoadFile` 而非 `LoadFrom`，避免 probing/AssemblyResolve 递归（`AssemblyLoader.cs:152-155`），加载成功后按程序集名写入 `_resolvedCache`（`AssemblyLoader.cs:155-158`）。缺失文件则跳过（`AssemblyLoader.cs:152-153`）。
- **step 2 初始化搜索路径**：`InitializeSearchPath()`（`AssemblyLoader.cs:78-79`）+ `RefreshRuntimeEnvironmentPath()`（`AssemblyLoader.cs:80`，把搜索路径追加进进程 `PATH`，供本地依赖的 native 库查找，`AssemblyLoader.cs:107-116`）。
- **step 3 注册统一解析器**：`AppDomain.CurrentDomain.AssemblyResolve += Instance.ResolveAssembly`（`AssemblyLoader.cs:83`）。

搜索路径表 `BaseFolderPath`（`AssemblyLoader.cs:36-43`）——每个根目录带一个递归深度，`GetAllSubDirectories` 按深度递归收集子目录（`AssemblyLoader.cs:165-199`）：

| 根目录（相对 BaseDirectory） | 递归深度 |
|---|---|
| 程序根目录 `BaseDirectory` | 0 |
| `core/` | 0 |
| `libraries/` | 1 |
| `modules/` | 0 |
| `runtimes/` | 2 |

### 步骤 3：动态程序集解析（ResolveAssembly）

`AssemblyLoader.cs:211-228`，三级兜底：

1. `_resolvedCache` 缓存（`AssemblyLoader.cs:218-220`，含预加载的 6 个程序集）
2. 已加载程序集遍历（`AssemblyLoader.cs:221-224`，`ResolveAssemblyFromLoaded` 见 235-246）
3. 搜索路径加载（`AssemblyLoader.cs:225-227`）

`ResolveAssemblyFromSearchPaths`（`AssemblyLoader.cs:248-270`）：

- 先 `CacheNativeDirectory(assemblyName)`（`AssemblyLoader.cs:251`）：在 `libraries/<包名前缀>/` 下递归找 native 资产（`.dylib/.so/.dll`，dll 需文件名含 "native" 或路径含 `/runtimes/`），缓存其目录（`AssemblyLoader.cs:278-295`），供后续 P/Invoke 兜底。
- 取程序集名第一段（如 `Avalonia.Controls` → `Avalonia`）匹配含该段的搜索路径（`AssemblyLoader.cs:253-254`，命中 `libraries/Avalonia/` 等分类目录）；
- 再对全部搜索路径逐个 `LoadAssembly(path, name)`（`AssemblyLoader.cs:257-263`）。`LoadAssembly` 按 `.dll`/`.exe` 顺序尝试 `Assembly.LoadFrom`（`AssemblyLoader.cs:92-101`）。

### 步骤 4：native 库解析（P/Invoke 兜底）

Release 布局下 NuGet native 资产被归入 `runtimes/<rid>/native/`，默认 probing 找不到，故：

- `RegisterNativeResolvers()`（`AssemblyLoader.cs:308-315`）：为 **Launcher 自身程序集**注册 `DllImportResolver`（`AssemblyLoader.cs:313`）。此步只引用自身程序集，不触发第三方加载（`Launcher.cs:26` 注释）。
- `PreloadNativeLibraries()`（`AssemblyLoader.cs:349-369`）：扫描 `runtimes/<rid>/native/` 与 `libraries/` 下所有 `native` 目录，`NativeLibrary.TryLoad` 全部预加载并把 handle 按“去扩展名文件名”与“完整文件名”双键记入 `_loadedNativeHandles`（`AssemblyLoader.cs:364-367`），后续 P/Invoke 直接命中。
- `RegisterNativeResolversForAvalonia()`（`AssemblyLoader.cs:323-333`）：在 **AppBuilder 构造完成后**（`Launcher.cs:59-62`）为 SkiaSharp、HarfBuzzSharp 及（macOS 后端）`Avalonia.Native` 程序集注册同一解析器——因为 `typeof(SkiaSharp.SKImageInfo)` 等引用会触发程序集加载，必须等 Avalonia 加载完毕。
- `ResolveNativeLibrary`（`AssemblyLoader.cs:379-413`）四步兜底：① 预加载 handle（382）→ ② 程序集解析时缓存的同包 native 目录（386）→ ③ `runtimes/<rid>/native/`（393）→ ④ `libraries/` 下按当前 rid 的 `runtimes/<rid>/` 段搜索（400-409）。文件名模式按平台：Windows `x.dll`/`x`、macOS `libx.dylib`/`x.dylib`、Linux `libx.so`/`x.so`（`AssemblyLoader.cs:419-424`）。

### 步骤 5：Launcher.Run() → Avalonia 启动

`Launcher/Launcher.cs:44-49`：

- `[STAThread]`（`Launcher.cs:44`），先 `Logger.Information("Application startup.")`（`Launcher.cs:47`），再 `RunAvalonia(args)`。
- `RunAvalonia` 标记 `[MethodImpl(MethodImplOptions.NoInlining)]`（`Launcher.cs:56`）——注释说明目的是把 Avalonia 类型的 JIT 延迟到 `AssemblyLoader.Initialize()` 之后，确保预加载与 AssemblyResolve 先就位（`Launcher.cs:51-55`）。
- `BuildAvaloniaApp()`（`Launcher.cs:66-73`）：`AppBuilder.Configure<WorkstationApplication>()` + `UsePlatformDetect()` + `WithInterFont()` + `LogToTrace()` + `WithDeveloperTools()`。随后注册 Avalonia 侧 native 解析器（`Launcher.cs:62`），`builder.StartWithClassicDesktopLifetime(args)`（`Launcher.cs:63`）。

### 步骤 6：FrameworkApplication 初始化（Prism 生命周期）

`WorkstationApplication`（`Modules/Workstation/WorkstationApplication.cs:6`）继承 `FrameworkApplication<MainWindow>`，只重写 `ConfigureModuleCatalog`（见步骤 8）。

`Core/Framework/FrameworkApplication.cs` 的关键覆写：

1. **`Initialize()`**（`FrameworkApplication.cs:20-27`）：设 `RequestedThemeVariant = ThemeVariant.Default`（:22），把 `WorkstationTheme` 加入 `Styles`（:23，主题来自 UIPackage，见 §3.5），再调 `base.Initialize()`。
2. **`RegisterTypes()`**（`FrameworkApplication.cs:83-87`）→ `RegisterFrameworkServices()`（:57-67）：
   - `IoC.Initialize(containerRegistry, Container)` 初始化全局 IoC 桥接（:59，`Core/Common/IoC.cs:64-72`，重复初始化会抛 `InvalidOperationException`，`IoC.cs:66-70`）；
   - 创建**同一个** `FrameworkWindowManager` 实例分别注册为 `IMainWindowManager` 与 `IWindowManager` 两个单例（:62-64）；
   - `ResolveFrameworkServices()` 从容器解析 `IEventAggregator` 与 `IMainWindowManager`（:69-75）。
   - 子类可覆写 `RegisterCustomService` 追加注册（:95-98，当前为空）。
3. **`CreateShell()`**（`FrameworkApplication.cs:104-107`）：从容器解析 `TWindow`（即 `MainWindow`）。
4. **`InitializeShell()`**（`FrameworkApplication.cs:109-116`）：
   - 订阅 `ShowMainWindowEvent` → `ShowMainWindow`（:114，`ThreadOption.UIThread, true`）；
   - `_windowManager.HandleMainWindow()` 登记主窗口（:115）。
5. **`ConfigureViewModelLocator()`**（`FrameworkApplication.cs:126-154`）：自定义 View→ViewModel 解析器——`Views`→`ViewModels` 命名空间替换（:140），`Window`/`Page` 后缀追加 `ViewModel`（:141-144），`View` 后缀追加 `Model`（:146-149），再按 `类型名, 程序集名` 做 `Type.GetType`（:151-153）。约定即：
   `**/Views/*View.xaml → **/ViewModels/*ViewModel.cs`（`FrameworkApplication.cs:118-123` 注释）。
6. `OnInitialized()` 与 `OnFrameworkInitializationCompleted()` 均被覆写为空（`FrameworkApplication.cs:30-32, 37-40`）——Prism/Avalonia 的默认收尾行为被替换，不做任何事。

### 步骤 7：窗口生命周期事件

- `ShowMainWindow`（`FrameworkApplication.cs:42-49`）：校验 `MainWindow` 与 `IClassicDesktopStyleApplicationLifetime`（:44-45），把 MainWindow 设为 `lifetime.MainWindow`（:46），`ShowMainWindow()`（:47），`CloseWindowsExceptMain()` 关闭其余窗口（:48）。
- `FrameworkWindowManager`（`Core/Framework/WindowManager/FrameworkWindowManager.cs:12`）维护 `Type→Window` 映射（:17）与 `_mainWindow`（:22）：
  - `GetWindow(Type)` 经 `IoC.Provider.Resolve` 从容器解析窗口（:35-38）；
  - `ShowWindow(Window)` 先 `InitializeWindow`（登记映射、挂 Closing 清理，:45-53）再以主窗口为 owner 显示（:78-88）；
  - `HandleMainWindow()` 取 `Application.Current.MainWindow` 登记为主窗口（:158-167）；
  - `ShowWindow`/`ShowDialog`/`CloseWindow`/`HideWindow` 及泛型扩展（`Core/Abstractions/WindowManager/IWindowManagerExtenstion.cs:22-53`）。

### 步骤 8：模块激活（Prism ModuleCatalog）

`WorkstationApplication.ConfigureModuleCatalog`（`WorkstationApplication.cs:8-11`）：`moduleCatalog.AddModule<DashBoardModule>()`。

`DashBoardModule`（`Modules/DashBoard/DashBoardModule.cs:6-17`，Prism `IModule`）：

- `RegisterTypes` 为空（:8-10）；
- `OnInitialized`（:12-16）：`containerProvider.Resolve<IWindowManager>()` → `windowManager.ShowWindow<DashBoardWindow>()`（泛型扩展见 `IWindowManagerExtenstion.cs:31-33`）。

**模块激活结果**：启动后首先可见的是 `DashBoardWindow`（由模块在 Prism 模块初始化阶段弹出）；`MainWindow` 仅被登记、不显示，直到点击 DashBoard 中的按钮（见步骤 9）。

**[推断]** Prism 的模块初始化（`InitializeModules`）发生在 `InitializeShell` 之后，因此 `HandleMainWindow`（步骤 6.4）先于 `DashBoardModule.OnInitialized` 执行，`ShowWindow` 才能拿到已登记的 `_mainWindow`。Prism 内部调用顺序不在本仓库源码中，属框架行为（README 也以该顺序描述，`README.md:43-49`）。

### 步骤 9：事件驱动的主窗口显示

- `DashBoardWindow.axaml` 声明 `prism:ViewModelLocator.AutoWireViewModel="True"`（`Modules/DashBoard/Views/Windows/DashBoardWindow.axaml:3`），按钮绑定 `ShowMainWindowCommand`（`DashBoardWindow.axaml:11`）。
- `DashBoardWindowViewModel`（`Modules/DashBoard/ViewModels/Windows/DashBoardWindowViewModel.cs:7-13`）：主构造注入 `IEventAggregator`（:7），`[RelayCommand] ShowMainWindow()` 发布 `ShowMainWindowEvent`（:10-12）。
- `ShowMainWindowEvent`（`Core/Models/Events/ShowMainWindowEvent.cs:3`）继承 Prism `PubSubEvent`。
- 事件回到 `FrameworkApplication.ShowMainWindow`（步骤 7），把 MainWindow 设为桌面生命周期主窗口并关闭 DashBoardWindow。

### 启动链路小结（一图流）

```
Launcher.exe (Program.Main, Program.cs:13)
 └─ Launcher.Initialize() (Launcher.cs:21)
     ├─ AssemblyLoader.Initialize() (AssemblyLoader.cs:62)
     │    ├─ [Release] 预加载 6 个引导 DLL (AssemblyLoader.cs:76)
     │    ├─ 初始化搜索路径 core|libraries|modules|runtimes (AssemblyLoader.cs:79)
     │    └─ 注册 AppDomain.AssemblyResolve (AssemblyLoader.cs:83)
     ├─ RegisterNativeResolvers() (AssemblyLoader.cs:308)
     └─ PreloadNativeLibraries() (AssemblyLoader.cs:349)
 └─ Launcher.Run(args) [STAThread] (Launcher.cs:45)
     └─ RunAvalonia (NoInlining, Launcher.cs:57)
         ├─ BuildAvaloniaApp → WorkstationApplication (Launcher.cs:66-73)
         ├─ RegisterNativeResolversForAvalonia() (AssemblyLoader.cs:323)
         └─ StartWithClassicDesktopLifetime (Launcher.cs:63)
             └─ Prism: RegisterTypes → IoC.Initialize + IWindowManager 单例 (FrameworkApplication.cs:57-67)
                 ├─ CreateShell → MainWindow (FrameworkApplication.cs:104)
                 ├─ InitializeShell → 订阅 ShowMainWindowEvent + HandleMainWindow (FrameworkApplication.cs:109-116)
                 └─ InitializeModules → DashBoardModule.OnInitialized
                     └─ IWindowManager.ShowWindow<DashBoardWindow> (DashBoardModule.cs:15)
                         → 点击按钮 → ShowMainWindowEvent → ShowMainWindow() (FrameworkApplication.cs:42)
```

---

## 3. Core/ 子目录职责

### 3.1 Abstractions —— 抽象层

- 职责：定义框架与模块之间的接口契约，不包含实现。程序集 `DigitalWorkstation.Core.Abstractions`（`Base.props:39-42`）。
- 内容（`Core/Abstractions/WindowManager/`）：
  - `IWindowManager`：窗口显示/隐藏/关闭/对话框的完整接口（`IWindowManager.cs:9-113`）。
  - `IMainWindowManager`：主窗口登记、显示、隐藏、关闭其他窗口（`IMainWindowManager.cs:8-28`）。
  - `IWindowManagerExtenstion`（原文拼写）：`IWindowManager` 的泛型扩展方法（`GetWindow<T>/ShowWindow<T>/ShowDialog<T>/HideWindow<T>/CloseWindow<T>`，`IWindowManagerExtenstion.cs:22-53`）。
- 依赖：仅 Avalonia 包（`Abstractions.csproj:10`）。
- 消费者：`FrameworkWindowManager` 实现这两个接口（`FrameworkWindowManager.cs:12`）；`DashBoardModule.OnInitialized` 消费 `IWindowManager`（`DashBoardModule.cs:14`）。

### 3.2 Common —— 通用基础设施

- 职责：全局静态基础设施，程序集 `DigitalWorkstation.Core.Common`。
- 内容：
  - `Logger`：Serilog 单例封装，Release 下最小级别 `Information`（`Logger.cs:34-38`），`#if DEBUG` 下 `Verbose`（`Logger.cs:41-45`），控制台输出模板 `[{Timestamp:HH:mm:ss} {Level:u3}] …`（`Logger.cs:37,44`），提供 `Verbose/Debug/Information/Warning/Error/Fatal` 静态入口（`Logger.cs:49-105`）。在启动时被 `Launcher.Run` 消费（`Launcher.cs:47`）。
  - `IoC`：Prism 容器（`IContainerRegistry`/`IContainerProvider`）的静态桥接单例，`Initialize` 后全局可用、重复初始化抛异常（`IoC.cs:64-72`）。被 `FrameworkWindowManager.GetWindow` 消费（`FrameworkWindowManager.cs:37`），被 `FrameworkApplication.RegisterFrameworkServices` 初始化（`FrameworkApplication.cs:59`）。
- 依赖：Prism.Avalonia、Prism.DryIoc.Avalonia、Serilog、Serilog.Sinks.Console（`Common.csproj:10-13`），及 Abstractions（`Common.csproj:17`）。

### 3.3 Framework —— 核心框架层

- 职责：Prism 应用基类与窗口管理的具体实现，程序集 `DigitalWorkstation.Core.Framework`。
- 内容：
  - `FrameworkApplication<TWindow> : PrismApplication`：主题装配（`FrameworkApplication.cs:23`）、IoC 初始化与 `IWindowManager`/`IMainWindowManager` 单例注册（:59-64）、ViewModelLocator 自动绑定（:126-154）、`ShowMainWindowEvent` 订阅与主窗口生命周期接管（:42-49, 109-116）。
  - `FrameworkWindowManager : IWindowManager, IMainWindowManager`：`Type→Window` 映射、窗口从容器解析、以主窗口为 owner 的 Show/ShowDialog、主窗口登记（`FrameworkWindowManager.cs:12, 35-38, 45-53, 78-88, 158-167`）。
- 依赖：Abstractions、Common、Models、UIPackage（`Framework.csproj:10-13`）；Avalonia.Desktop、Fonts.Inter、Themes.Fluent、DiagnosticsSupport、CommunityToolkit.Mvvm 包（`Framework.csproj:17-21`）。
- 消费者：`WorkstationApplication`（`WorkstationApplication.cs:6`）、`DashBoardModule`（经 `DashBoard.csproj:18` 引用）。

### 3.4 Models —— 跨模块模型/事件

- 职责：跨模块共享的 Prism 事件，程序集 `DigitalWorkstation.Core.Models`。
- 内容：`ShowMainWindowEvent : PubSubEvent`（`ShowMainWindowEvent.cs:3`）。
- 依赖：Common（`Models.csproj:10`）。
- 消费者：发布方 `DashBoardWindowViewModel`（`DashBoardWindowViewModel.cs:12`）；订阅方 `FrameworkApplication.InitializeShell`（`FrameworkApplication.cs:114`）。

### 3.5 UIPackage —— UI 资源包

- 职责：聚合 UI 主题，程序集 `DigitalWorkstation.Core.UIPackage`。
- 内容：`WorkstationTheme : Styles`，构造时依次加入 `SemiTheme`、`Ursa.Themes.Semi.SemiTheme`、`ColorPickerSemiTheme`、`DataGridSemiTheme`（`WorkstationThemes.cs:8-15`）。
- 依赖：Avalonia、Semi.Avalonia 及 ColorPicker/DataGrid 扩展、Irihi.Ursa 及 Semi 主题包（`UIPackage.csproj:10-15`）。
- 消费者：`FrameworkApplication.Initialize` 把 `WorkstationTheme` 加入全局 `Styles`（`FrameworkApplication.cs:23`）。
- 注：README 称其“目前仅工程骨架”（`README.md:21`），但代码中已含 `WorkstationTheme`，文档略滞后。

---

## 4. Modules/ 模块职责

### 4.1 Workstation —— 主应用

- 职责：Avalonia `Application` 与主窗口，负责 Prism 模块目录装配。
- 关键类型：
  - `WorkstationApplication : FrameworkApplication<MainWindow>`（`WorkstationApplication.cs:6`），唯一覆写是 `ConfigureModuleCatalog` → `AddModule<DashBoardModule>()`（`WorkstationApplication.cs:8-11`）。
  - `MainWindow : Window`（`MainWindow.axaml.cs:5-10`），无 ViewModel、无 `AutoWireViewModel`（`MainWindow.axaml:1-12`），仅显示 “Welcome Digital.Workstation” 文本（`MainWindow.axaml:11`）。
- 依赖：Framework、UIPackage、DashBoard（`Workstation.csproj:10-12`）。
- 与 Launcher 的关系：Launcher 以 ProjectReference 引用 Workstation（`Launcher.csproj:12`），`AppBuilder.Configure<WorkstationApplication>` 直接指名（`Launcher.cs:68`）。

### 4.2 DashBoard —— 示例业务模块

- 职责：演示 Prism 模块激活与事件流——启动后首先弹出自己的窗口，并可触发主窗口显示。
- 关键类型：
  - `DashBoardModule : IModule`（`DashBoardModule.cs:6`）：`OnInitialized` 中解析 `IWindowManager` 并 `ShowWindow<DashBoardWindow>()`（`DashBoardModule.cs:12-16`）。
  - `DashBoardWindow : Window`（`DashBoardWindow.axaml.cs:5-10`），XAML 启用 `AutoWireViewModel`（`DashBoardWindow.axaml:3`），含 “Show Main Window” 按钮（`DashBoardWindow.axaml:11`）。
  - `DashBoardWindowViewModel`（`DashBoardWindowViewModel.cs:7-13`）：构造注入 `IEventAggregator`，`ShowMainWindowCommand` 发布 `ShowMainWindowEvent`。
- 依赖：Abstractions、Framework（`DashBoard.csproj:17-18`）。
- **加载方式**：不是运行时按目录发现，而是**编译期**通过 Workstation→DashBoard 的 ProjectReference（`Workstation.csproj:12`）进入依赖图；Launcher 运行时按名解析 `DigitalWorkstation.DashBoard.dll` 时，由 `AssemblyResolve` 从 `modules/` 搜索路径补位（见 §5）。

---

## 5. 构建与部署（输出布局如何支撑启动逻辑）

### 5.1 输出布局的生成

- 统一输出根：`BaseOutputPath = $(SolutionDir)Output\$(Configuration)\`（`Build/Base.props:8`），关闭 RID/目标框架追加路径（`Base.props:9-10`）。独立用 csproj 构建时 `SolutionDir` 回退到仓库根（`Base.props:4`）。
- Release 下按项目类别分目录（`Base.props:49-55`）：
  - `Core/` 项目 → `Output\Release\core\`（`Base.props:49-51`）
  - `Modules/` 项目 → `Output\Release\modules\`（`Base.props:53-55`）
  - 其余（Launcher）→ `Output\Release\`（`Base.props:60-62`）
- Release 额外行为：
  - 不输出 PDB/XML 文档/deps 文件（`Base.props:28-31`）；
  - 项目引用不复制到本地（`Private=false`，`ManageDlls.props:3-5`）——这正是 Launcher 输出根**没有** Workstation.dll，必须靠 AssemblyResolve 去 `modules/` 找的原因；
  - `CopyDependenciesByPackageCategory`（`ManageDlls.targets:2-37`）：把 `RuntimeCopyLocalItems`+`NativeCopyLocalItems` 按 `NuGetPackageId` 第一段分类复制到 `libraries/<分类>/`（`ManageDlls.targets:11-25`），无包信息文件进 `libraries/Others/`（`ManageDlls.targets:28-36`）。实测 `libraries/` 下含 `Serilog/`、`Avalonia/`、`Semi/`、`Irihi/`、`Prism/`、`SkiaSharp/` 等分类目录（`Output/Release/libraries/`）。
  - `ClearDllFiles`（`ManageDlls.targets:39-63`）：构建后删除输出根目录的 NuGet DLL（:39-47），并清理 `runtimes/` 下非 `linux-x64`/`osx`/`win-x64` 的 RID 目录（:48-62）。实测 Release 的 `runtimes/` 恰余这三个 RID，各含 `native/`（`Output/Release/runtimes/`）。
- Debug 构建：上述 Release 专属 target/属性均不生效，依赖默认规则平铺输出——实测 `Output/Debug/` 下所有 DLL 平铺在根、`runtimes/` 保留全部 RID（`Output/Debug/`），与 `IsDesignEnvironment()` 在 DEBUG 下跳过自定义解析的设计自洽。

### 5.2 实测 Release 布局与加载器的对应关系

| 目录（`Output/Release/`） | 内容 | 被谁消费 |
|---|---|---|
| 根 | `Launcher.exe/.dll`、`Launcher.runtimeconfig.json` | 进程入口 |
| `core/` | `DigitalWorkstation.Core.*.dll` ×5 | 搜索路径（深度 0），`AssemblyLoader.cs:39` |
| `libraries/<包前缀>/` | NuGet 托管 DLL 按分类归档 | 搜索路径（深度 1），`AssemblyLoader.cs:40`；`BootRequiredAssemblyFiles` 的 Serilog/Avalonia 项直接按此路径预加载（`AssemblyLoader.cs:25-30`） |
| `modules/` | `DigitalWorkstation.Workstation.dll`、`DigitalWorkstation.DashBoard.dll` | 搜索路径（深度 0），`AssemblyLoader.cs:41`——模块 DLL 靠 AssemblyResolve 在此被找到 |
| `runtimes/<rid>/native/` | `libSkiaSharp.dll`、`libHarfBuzzSharp.dll`、`av_libglesv2.dll` 等 | 搜索路径（深度 2），`AssemblyLoader.cs:42`；`NativeLibraryDir` 预加载（`AssemblyLoader.cs:338-342, 349-356`） |

### 5.3 其他构建事实

- SDK 固定 `10.0.101`，`rollForward: latestMajor`、允许 prerelease（`global.json:3-5`）。
- 解决方案 `Digital.Workstation.slnx` 收录 5 个 Core 项目、2 个 Modules 项目、Launcher，以及 Build 文件与 README（`Digital.Workstation.slnx:3-26`）。
- `Directory.Build.props` 导入 `Build/Base.props` + `Build/ManageDlls.props`（`Directory.Build.props:2-5`），`Directory.Build.targets` 导入 `Base.targets` + `ManageDlls.targets`（`Directory.Build.targets:2-5`），排除 UnitTest 目录；`Base.targets` 为空壳（`Build/Base.targets:1-2`）。
- 依赖方向：Launcher → Workstation → {Framework, UIPackage, DashBoard}；DashBoard → {Abstractions, Framework}；Framework → {Abstractions, Common, Models, UIPackage}；Common → Abstractions；Models → Common。无循环。

---

## 6. 未决问题（源码无法直接确定的点）

1. **README 与现状不一致**：README 称 .NET 8（`README.md:7`），实际 `global.json:3` 为 SDK 10.0.101、全部 csproj 为 `net10.0`；README 提及 `Digital.Workstation.sln`（`README.md:34,56,60`），实际解决方案文件是 `Digital.Workstation.slnx`。属文档滞后，非代码问题。
2. **首次可见窗口**：启动后 MainWindow 只被 `HandleMainWindow` 登记、从不显示，首个可见窗口是 DashBoardWindow。源码未注明这是否为最终产品形态（DashBoard 是“示例模块”，`README.md:24`）；关闭 DashBoardWindow 的唯一途径是点击按钮发布 `ShowMainWindowEvent`。
3. **Prism 模块初始化时序**：`InitializeModules` 相对 `InitializeShell` 的顺序来自 Prism.Avalonia 框架行为，仓库内无源码佐证 **[推断]**。若时序相反，`ShowWindow<DashBoardWindow>` 会因 `_mainWindow == null` 抛 `InvalidOperationException`（`FrameworkWindowManager.cs:85-87`）——现有代码正常工作的前提即该推断成立。
4. **DashBoardWindow 的容器解析**：`DashBoardModule.RegisterTypes` 为空（`DashBoardModule.cs:8-10`），`GetWindow` 却直接 `IoC.Provider.Resolve(type)`（`FrameworkWindowManager.cs:35-38`）。能解析成功依赖 DryIoc 对未注册具体类型的默认解析能力 **[推断]**。
5. **native 资产归档归属**：`CopyDependenciesByPackageCategory` 源码把 `NativeCopyLocalItems` 一并扁平复制进 `libraries/<分类>/`（`ManageDlls.targets:11-25`），但实测 `libraries/SkiaSharp/` 只含托管 `SkiaSharp.dll`，native 资产（`libSkiaSharp.dll` 等）只出现在 `runtimes/win-x64/native/`。二者存在出入，可能源于 .NET SDK 对 runtimes 资产的独立复制机制；对加载逻辑的影响是 `CacheNativeDirectory` 在 `libraries/<包>/` 下未必能找到 native 资产（`AssemblyLoader.cs:278-295`），实际命中依赖第 ③④ 步兜底（`AssemblyLoader.cs:393-409`）。
6. **`IsDesignEnvironment` 的双重语义**：`#if DEBUG` 下恒 true（`AssemblyLoader.cs:140-141`），意味着**任何** Debug 运行（含正常调试）都跳过自定义解析，并非仅设计器场景。Release 运行才走完整引导流程。
7. **空壳/占位实现**：`InitializeCore` 为 TODO（`Launcher.cs:32-35`）；`OnInitialized` 与 `OnFrameworkInitializationCompleted` 被覆写为空（`FrameworkApplication.cs:30-40`），Prism 默认收尾被整体替换，后续初始化逻辑需要自行接入。
8. **两套加载上下文**：预加载用 `Assembly.LoadFile`（`AssemblyLoader.cs:155`），动态解析用 `Assembly.LoadFrom`（`AssemblyLoader.cs:101`），同名程序集理论上可能存在于不同上下文（默认上下文 vs LoadFrom 上下文）；`ResolveAssemblyFromLoaded` 的遍历（`AssemblyLoader.cs:235-246`）可缓解重复加载，但缓存键均为程序集名，存在同名单实例互斥的细节未在源码中说明。
