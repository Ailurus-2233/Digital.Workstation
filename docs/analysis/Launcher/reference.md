# Launcher — 模块关系链

## 依赖关系

### 项目引用（Launcher.csproj:11-13）

唯一参与编译和运行时依赖的直接项目引用：

| 依赖 | 用到的能力 | 本模块使用点 |
|---|---|---|
| `Modules/Workstation`（Workstation.csproj） | `WorkstationApplication`——Avalonia Application 类（Workstation/WorkstationApplication.cs，继承 `FrameworkApplication<MainWindow>`） | `Launcher.BuildAvaloniaApp()`（Launcher.cs:68）`AppBuilder.Configure<WorkstationApplication>()` |

此外，Launcher.csproj 自动包含 Plugins/**/*.csproj 的构建引用，设置 ReferenceOutputAssembly=false、Private=false、PrivateAssets=all、ExcludeAssets=all。dotnet run/F5 会先构建插件，但宿主不静态引用其类型，也不把插件私有包资产并入自身依赖；插件加载仍依靠目录扫描。

### 传递依赖（未在 csproj 直接引用，源码 using 其命名空间）

| 传递来源 | 用到的能力 | 使用点 |
|---|---|---|
| `Core/Common`（经 Workstation 传递链） | `Logger.Information(string, string)` 静态日志门面 | `Launcher.Run`（Launcher.cs:47）`Logger.Information("Application startup.", nameof(Launcher))`；`Launcher.cs:3` `using DigitalWorkstation.Core.Common` |
| Avalonia（经 Workstation → Framework 传递） | `AppBuilder`、`UsePlatformDetect/WithInterFont/LogToTrace/WithDeveloperTools` 扩展、`StartWithClassicDesktopLifetime` | `Launcher.cs:3` `using Avalonia`；`BuildAvaloniaApp`（:66-73）与 `RunAvalonia`（:57-64）；`Program.cs:3` 与 `:23` |
| SkiaSharp / HarfBuzzSharp / Avalonia.Native（NuGet，经传递） | 仅以 `typeof(...)` 取程序集对象注册 native resolver：`SkiaSharp.SKImageInfo`、`HarfBuzzSharp.Blob`；`Avalonia.Native` 因目标类 internal 改按程序集名从 `AppDomain.GetAssemblies()` 查找 | `AssemblyLoader.RegisterNativeResolversForAvalonia`（AssemblyLoader.cs:323-333） |
| .NET BCL | `System.Reflection`（`Assembly`/`AssemblyName`）、`System.Runtime.InteropServices`（`NativeLibrary`/`DllImportResolver`/`RuntimeInformation`/`OSPlatform`）、`System.Collections.Concurrent`（`ConcurrentDictionary`）、`AppDomain.AssemblyResolve`、`AppContext.BaseDirectory` | AssemblyLoader.cs:1-3 using 及全文件 |

### 编译设置与构建入口

Launcher 使用 Microsoft.NET.Sdk、WinExe、net10.0，显式 AssemblyName=Launcher；ApplicationIcon=Assets\AppIcon.ico 经 Avalonia targets 注册默认 Window.Icon。仓库根的 Directory.Build.props/targets 统一加载 Build/Base.*、Build/ManageDlls.*，仅 Plugins 项目额外导入 Build/Plugins.targets。

支持的分发入口是 dotnet build Digital.Workstation.slnx -c Release 或 dotnet build Launcher/Launcher.csproj -c Release，产物位于 Output/Release/。插件不会要求用户先手动构建整个解决方案：Launcher 中的构建引用负责构建 Plugins 下的项目。当前没有独立的 dotnet publish 目录编排流程。

### Release 布局与依赖归属

| 构建行为 | 运行时消费方 |
|---|---|
| Build/Base.props 将 Launcher 输出到根，Core 输出到 core/，Modules 输出到 modules/ | Launcher/AssemblyLoader 按原有宿主目录准备运行时依赖 |
| 宿主的 Build/ManageDlls.props 禁止复制项目引用；ManageDlls.targets 将 NuGet 资产归档到 libraries/<包首段>/ 并清理宿主项目输出目录中的重复 DLL | AssemblyLoader 的全局缓存和宿主搜索路径只服务宿主依赖 |
| Plugins 路径识别兼容 Windows 和 Unix 分隔符，Release 输出到 plugins/$(MSBuildProjectName)/ | Framework 的插件发现仅扫描 plugins 的直接子目录，各目录对应一个插件包 |
| Plugins 项目启用 EnableDynamicLoading、GenerateDependencyFile、CopyLocalLockFileAssemblies；跳过宿主的归档与清理规则 | 插件入口旁的 .deps.json 和 SDK 原有目录结构供 AssemblyDependencyResolver 解析私有 managed/native 资产及卫星资源 |
| Build/Plugins.targets 在 ResolveReferences 后过滤 ReferenceCopyLocalPaths；程序集文件名及 NuGetPackageId 匹配共享清单的资产不重复复制，Modules 项目引用也由宿主提供 | 共享 Core、Prism、Avalonia、Serilog 等实际程序集对象保持类型身份一致；每个插件的其他依赖由其加载上下文拥有 |
| Build/PluginSharedAssemblies.txt 逐行列出宿主实际携带的共享程序集精确简单名及其 native package ID，仅 DigitalWorkstation.Core.* 使用前缀；大小写不敏感，构建读取、Framework 嵌入同一文件 | 构建过滤和运行时共享边界只维护一份清单；不能把全部宿主 NuGet 库无条件视为共享契约 |
| Debug 所有 DLL 平铺 Output/Debug/，不执行 Release 插件共享过滤 | 插件发现扫描程序根目录；同名多版本依赖的物理隔离只在 Release 布局验证 |

插件私有依赖不加入 AssemblyLoader.BaseFolderPath、进程 PATH 或宿主 native 句柄缓存。修改宿主分类目录时继续同步 Build/ManageDlls 与 AssemblyLoader；修改插件目录和打包规则时同步 Build/Plugins 与 Framework 插件加载逻辑。新增共享 UI/契约依赖时检查共享清单；第三方条目只使用宿主实际携带的精确名称，避免 Serilog.* 一类通配误删 Serilog.Sinks.File 等插件私有扩展。不通过扩大全局搜索范围补救缺文件。

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
