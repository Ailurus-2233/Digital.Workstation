# Launcher — 模块关系链

## 依赖关系

### 项目引用（Launcher.csproj:11-13）

唯一直接项目引用：

| 依赖 | 用到的能力 | 本模块使用点 |
|---|---|---|
| `Modules/Workstation`（Workstation.csproj） | `WorkstationApplication`——Avalonia Application 类（Workstation/WorkstationApplication.cs，继承 `FrameworkApplication<MainWindow>`） | `Launcher.BuildAvaloniaApp()`（Launcher.cs:68）`AppBuilder.Configure<WorkstationApplication>()` |

### 传递依赖（未在 csproj 直接引用，源码 using 其命名空间）

| 传递来源 | 用到的能力 | 使用点 |
|---|---|---|
| `Core/Common`（经 Workstation 传递链） | `Logger.Information(string, string)` 静态日志门面 | `Launcher.Run`（Launcher.cs:47）`Logger.Information("Application startup.", nameof(Launcher))`；`Launcher.cs:3` `using DigitalWorkstation.Core.Common` |
| Avalonia（经 Workstation → Framework 传递） | `AppBuilder`、`UsePlatformDetect/WithInterFont/LogToTrace/WithDeveloperTools` 扩展、`StartWithClassicDesktopLifetime` | `Launcher.cs:3` `using Avalonia`；`BuildAvaloniaApp`（:66-73）与 `RunAvalonia`（:57-64）；`Program.cs:3` 与 `:23` |
| SkiaSharp / HarfBuzzSharp / Avalonia.Native（NuGet，经传递） | 仅以 `typeof(...)` 取程序集对象注册 native resolver：`SkiaSharp.SKImageInfo`、`HarfBuzzSharp.Blob`；`Avalonia.Native` 因目标类 internal 改按程序集名从 `AppDomain.GetAssemblies()` 查找 | `AssemblyLoader.RegisterNativeResolversForAvalonia`（AssemblyLoader.cs:323-333） |
| .NET BCL | `System.Reflection`（`Assembly`/`AssemblyName`）、`System.Runtime.InteropServices`（`NativeLibrary`/`DllImportResolver`/`RuntimeInformation`/`OSPlatform`）、`System.Collections.Concurrent`（`ConcurrentDictionary`）、`AppDomain.AssemblyResolve`、`AppContext.BaseDirectory` | AssemblyLoader.cs:1-3 using 及全文件 |

### 编译设置（Launcher.csproj:1-14）

`Microsoft.NET.Sdk`、`OutputType=WinExe`（:4）、`ImplicitUsings`+`Nullable` enable（:5-6）、`TargetFramework=net10.0`（:7）、**显式 `AssemblyName=Launcher`**（:8）——覆盖了 `Build/Base.props:35` 默认会拼出的 `DigitalWorkstation.Launcher`（Launcher 不在 `Core/`/`Modules/`/`UnitTest/` 任何一路径规则内）。csproj 不 import 任何 Build 脚本；仓库根的 `Directory.Build.props`/`Directory.Build.targets` 统一 import `Build/Base.props`、`Build/Base.targets`、`Build/ManageDlls.props`、`Build/ManageDlls.targets`。

### 发布布局（Release）——与运行时解析的对偶关系

Launcher 是 `Build/ManageDlls.*` 布局约定的**消费方**，其搜索路径必须与之一一对应：

| 构建侧行为（Build/） | 运行时侧对应（Launcher/） |
|---|---|
| `Base.props:8` `BaseOutputPath = $(SolutionDir)Output\$(Configuration)\`；Launcher 不命中 core/modules/unittest 任何条件，走 `:62` 默认 → 输出到根 | `AssemblyLoader.BaseDirectory = AppContext.BaseDirectory`（:18）即输出根 |
| `ManageDlls.props:3-5` Release 下 `ProjectReference.Private=false`：项目引用 DLL **不复制**到输出根；`Base.props:50/54` 把 Core 项目输出到 `core\`、Modules 项目输出到 `modules\` | `BaseFolderPath`（:39、41）含 `core/`、`modules/`；`BootRequiredAssemblyFiles` 里的 `Core/DigitalWorkstation.Core.Common.dll`（:27）按此布局书写 |
| `ManageDlls.targets` `CopyDependenciesByPackageCategory`（:2-37）：NuGet 运行期资产按 `NuGetPackageId.Split('.')[0]` 分类复制到 `libraries\<分类>\`，无包信息的进 `libraries\Others\` | `BaseFolderPath` 含 `libraries/`（递归深度 1，:40）；`ResolveAssemblyFromSearchPaths`（:253-254）按 `assemblyName.Split('.')[0]` 优先定位同名分类目录（如 `Serilog.Sinks.Console` → `libraries/Serilog`） |
| `ManageDlls.targets` `ClearDllFiles`（:39-63）：删除输出根的项目 DLL，`runtimes/` 下只保留 `linux-x64`/`osx`/`win-x64` 三个 rid 目录 | `NativeLibraryDir`（:338-342）只取 `win-x64`/`osx`/`linux-x64` 三者之一；`ResolveNativeLibrary` 兜底搜索（:403-404）同样限定这三个 rid |
| Debug 构建不做上述分类，全部 DLL 平铺输出根 | `IsDesignEnvironment()`（:138-146）DEBUG 恒 `true`，整个引导器禁用，靠默认 probing |

结论：Release 布局 = `Launcher.exe` 在根 + `core/` + `modules/` + `libraries/<分类>/` + `runtimes/<rid>/native/`。**改 ManageDlls 分类规则必须同步 `BaseFolderPath`/`BootRequiredAssemblyFiles`，反之亦然。**

## 被依赖关系

| 依赖方 | 引用方式 | 用在什么场景 |
|---|---|---|
| 操作系统 / `dotnet Launcher.dll` | WinExe 入口点 | `Program.Main` 是整个解决方案的进程入口 |
| Avalonia 设计器/预览器 | 约定入口反射调用 | `Program.BuildAvaloniaApp()`（Program.cs:23）被预览器宿主调用构建 `AppBuilder` |
| `Digital.Workstation.slnx` | 解决方案成员 | 启动项目 |

**没有任何项目 ProjectReference 到 Launcher**；它位于依赖图顶端（Launcher → Workstation → Framework → …）。

## 核心内部数据结构

`AssemblyLoader`（AssemblyLoader.cs:12，`sealed`，Lazy 单例 :47-53）的全部状态：

| 成员 | 类型 | 定义处 | 语义 |
|---|---|---|---|
| `BaseDirectory` | `static readonly string` | :18 | `AppContext.BaseDirectory`，一切相对路径的锚点 |
| `BootRequiredAssemblyFiles` | `static readonly string[]` | :23-31 | 启动预加载清单（6 项，见 api.md） |
| `BaseFolderPath` | `static readonly Dictionary<string,int>` | :36-43 | 搜索根 → 子目录递归深度（0=不递归，libraries=1，runtimes=2） |
| `SingleInstance` / `Instance` | `Lazy<AssemblyLoader>` | :47-49 | 单例；私有构造 :51 |
| `InitLock` / `SearchPathsLock` | `static readonly object` | :121-123 | 分别保护初始化与搜索路径重建 |
| `SourceArray` | `static readonly string[]` | :125 | `[".dll", ".exe"]` |
| `_resolvedCache` | `ConcurrentDictionary<string, Assembly>` | :127 | 简单名 → 已解析程序集；由 `PreloadBootAssemblies`（:158）、`ResolveAssemblyFromLoaded`（:241）、`ResolveAssemblyFromSearchPaths`（:260、267）三处写入 |
| `_searchPaths` | `List<string>` | :129 | 最终搜索路径全集；唯一写入方 `InitializeSearchPath`（:165-182，lock + Clear + AddRange）。构成 = `BaseFolderPath` 五个键 + 对每个**实际存在**（`Path.Exists` 过滤，:176）的基目录调 `GetAllSubDirectories(path, 该键深度值)` 的递归结果；`GetAllSubDirectories`（:187-202）签名默认 `limit = 4`，`limit == 0` 直接返回空停止递归，每深入一层 `limit - 1`——即递归层数恰等于 `BaseFolderPath` 里的值（libraries=1 只取一层包分类目录，runtimes=2 取到 rid/native 层） |
| `_isInitialized` / `_nativeResolversRegistered` | `bool` | :131-133 | 幂等守卫 |
| `_nativeDirs` | `static ConcurrentDictionary<string,string>` | :297 | 程序集简单名 → 同包附带 native 目录；唯一写入方 `CacheNativeDirectory`（:278-295，由 `ResolveAssemblyFromSearchPaths` 开头调用）。其规则：已含该键直接返回；枚举目录 = `Path.Combine(BaseDirectory, "libraries/", assemblyName.Split('.')[0])`（不存在则返回）；`Directory.EnumerateFiles(..., SearchOption.AllDirectories)` 递归枚举，扩展名白名单 `.dylib/.so/.dll`；对 `.dll` 额外要求文件名含 `"native"` **或**路径含 `/runtimes/`（均 OrdinalIgnoreCase），否则跳过；命中第一个合格文件即 `_nativeDirs[assemblyName] = Path.GetDirectoryName(file)` 并 return |
| `_loadedNativeHandles` | `static ConcurrentDictionary<string,IntPtr>`（`OrdinalIgnoreCase`） | :371-372 | native 文件名（去扩展名；dll 另有带扩展名键）→ 已加载句柄；写入方 `PreloadNativeLibraries`（:365-367） |
| `NativeLibraryDir` | `static readonly string` | :338-342 | `runtimes/<rid>/native/` |

关系：`ResolveAssembly`（:211）三级回退按 `_resolvedCache` → 已加载程序集 → `_searchPaths`+`LoadAssembly`；`ResolveNativeLibrary`（:379）四级回退按 `_loadedNativeHandles` → `_nativeDirs` → `NativeLibraryDir` → `libraries/` 全树限定 rid 搜索。两条链共享"先命中已加载、再按目录约定找文件"的同一策略。

native 链的两个私有工具方法：

- `NativeFilePatterns(string libraryName)`（:428-435）：平台判定用 `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)` / `IsOSPlatform(OSPlatform.OSX)`，其余归入 Linux 分支。返回候选文件名数组——Windows `["{name}.dll", "{name}"]`；macOS `["lib{name}.dylib", "{name}.dylib"]`；Linux `["lib{name}.so", "{name}.so"]`。
- `TryLoadFromDir(string dir, string libraryName)`（:416-426）：对 `NativeFilePatterns` 的每个候选，`Path.Combine(dir, file)` 拼全路径 → `File.Exists` 检查 → `NativeLibrary.TryLoad`；首个成功句柄即返回，全部失败返回 `IntPtr.Zero`。它是 `ResolveNativeLibrary` step 2（`_nativeDirs` 缓存目录）与 step 3（`NativeLibraryDir`）共用的"在指定目录内按名加载"原语；step 4 不用它，而是 `Directory.EnumerateFiles(librariesRoot, pattern, AllDirectories)` 全树搜索后用 `/runtimes/<rid>/` 过滤。

### 与外部类型的对应关系

| 本模块类型 | 对应外部类型（定义处） |
|---|---|
| `Launcher.BuildAvaloniaApp` 的泛型实参 | `WorkstationApplication`（Modules/Workstation/WorkstationApplication.cs），契约详见 docs/analysis/Modules/Workstation/api.md |
| `Launcher.Run` 的日志调用 | `DigitalWorkstation.Core.Common.Logger`（Core/Common），详见 docs/analysis/Core/Common/api.md |
| `Program.BuildAvaloniaApp` | Avalonia `AppBuilder`，预览器约定签名 `public static AppBuilder BuildAvaloniaApp()` |
