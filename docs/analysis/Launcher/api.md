# Launcher — 对外接口与调用方式

本模块是解决方案的入口程序集，"对外接口"的实际消费者是 CLR/操作系统（`Main`）、Avalonia 设计器/预览器（`BuildAvaloniaApp`）以及本程序集内部。没有任何其他项目引用 `Launcher`（它是依赖链顶端）。全部公开类型命名空间均为 `DigitalWorkstation.Launcher`。

## 公开类型与签名

### `public static class Program`（Program.cs:7-24）

| 成员 | 签名 | 说明 |
|---|---|---|
| `Main` | `private static void Main(string[] args)` | 进程入口（private，CLR 约定入口不需要 public）。顺序调 `Launcher.Initialize()` 与 `Launcher.Run(args)`（Program.cs:15-16）。**方法体内不引用任何 Avalonia 类型**，保证 JIT 该方法时不需要解析 Avalonia 程序集。 |
| `BuildAvaloniaApp` | `public static AppBuilder BuildAvaloniaApp()` | Avalonia 设计器/预览器约定入口（Program.cs:23），直接转发 `Launcher.BuildAvaloniaApp()`。设计时 `AssemblyLoader.Initialize()` 自动禁用（DEBUG 恒禁用，见 pitfalls.md），预览器从输出根目录平铺探测 DLL。 |

### `public static class Launcher`（Launcher.cs:16-73）

| 成员 | 签名 | 说明 |
|---|---|---|
| `Initialize` | `public static void Initialize()` | 引导流程（Launcher.cs:21-30）：`AssemblyLoader.Initialize()` → `RegisterNativeResolvers()` → `PreloadNativeLibraries()` → `InitializeCore()`（私有，Launcher.cs:32-35，当前为空，注释 `TODO 添加初始化逻辑`）。必须在任何 Avalonia/SkiaSharp 类型被 JIT 之前调用。 |
| `Run` | `[STAThread] public static void Run(string[] args)` | 主运行函数（Launcher.cs:44-49）：`Logger.Information("Application startup.", nameof(Launcher))`（`DigitalWorkstation.Core.Common.Logger`）后进入 `RunAvalonia(args)`。 |
| `BuildAvaloniaApp` | `public static AppBuilder BuildAvaloniaApp()` | （Launcher.cs:66-73）`AppBuilder.Configure<WorkstationApplication>().UsePlatformDetect().WithInterFont().LogToTrace().WithDeveloperTools()`。`WorkstationApplication` 来自 `Modules/Workstation`。 |

私有实现细节：`RunAvalonia(string[] args)`（Launcher.cs:56-64）标 `[MethodImpl(MethodImplOptions.NoInlining)]`，在 `AppBuilder` 构造完成后调 `AssemblyLoader.RegisterNativeResolversForAvalonia()`，最后 `builder.StartWithClassicDesktopLifetime(args)` 进入桌面主循环。

### `public sealed class AssemblyLoader`（AssemblyLoader.cs:12-438）

单例：`private static readonly Lazy<AssemblyLoader> SingleInstance`（:47），私有构造（:51），全部公开 API 是 static、内部转发到 `Instance`。公开静态方法：

| 方法 | 签名 | 行为 |
|---|---|---|
| `Initialize` | `public static void Initialize()` | （:62-87）`lock(InitLock)` + `_isInitialized` 守卫，幂等。DEBUG 下仅置标志即返回；Release 下：`PreloadBootAssemblies` → `InitializeSearchPath` → `RefreshRuntimeEnvironmentPath` → 注册 `AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly`。 |
| `LoadAssembly` | `public static Assembly? LoadAssembly(string path, string assemblyName)` | （:92-102）`path` 经 `Path.GetFullPath` 归一化，目录不存在返回 `null`；依次尝试 `{assemblyName}.dll`、`{assemblyName}.exe`（`SourceArray`，:125），首个存在的用 `Assembly.LoadFrom` 加载并返回；都不存在返回 `null`。 |
| `RefreshRuntimeEnvironmentPath` | `public static void RefreshRuntimeEnvironmentPath()` | （:107-115）读取 `Environment.GetEnvironmentVariable("PATH")`（null 时按 `""` 处理），按 `Path.PathSeparator` 分成分组列表；遍历 `Instance._searchPaths`，把其中**尚未被 PATH 包含**（`paths.Contains(p)` 判定）的每一项追加到列表末尾；最后 `Environment.SetEnvironmentVariable("PATH", string.Join(separator, paths))` 整体写回进程 PATH。方向单向（PATH 不回流 `_searchPaths`），目的是让 native 库的间接依赖能被默认搜索找到。调用位置：`Initialize` 的 step 2，紧跟 `InitializeSearchPath` 之后、`AssemblyResolve` 注册之前（:79-80）；也可独立再调，但未先 `Initialize` 时 `_searchPaths` 为空、等于无操作。 |
| `RegisterNativeResolvers` | `public static void RegisterNativeResolvers()` | （:308-316）`lock(InitLock)` + `_nativeResolversRegistered` 守卫；`NativeLibrary.SetDllImportResolver(typeof(AssemblyLoader).Assembly, ResolveNativeLibrary)`。只引用自身程序集，在 Avalonia 加载前调用安全。 |
| `RegisterNativeResolversForAvalonia` | `public static void RegisterNativeResolversForAvalonia()` | （:323-333）为 `typeof(SkiaSharp.SKImageInfo).Assembly`、`typeof(HarfBuzzSharp.Blob).Assembly` 及按名字找到的 `"Avalonia.Native"` 程序集注册同一 resolver。**必须在 Avalonia 程序集加载完毕后调用**（`RunAvalonia` 在 `AppBuilder` 构造后调它）。 |
| `PreloadNativeLibraries` | `public static void PreloadNativeLibraries()` | （:349-369）扫描 `runtimes/<rid>/native/`（`NativeLibraryDir`，:338-342）与 `libraries/` 下所有名为 `native` 的目录（`Directory.EnumerateDirectories(librariesRoot, "native", AllDirectories)`）。对每个目录枚举文件：扩展名不在 `.dylib/.so/.dll` 白名单的**跳过**（:361）；`NativeLibrary.TryLoad` 返回 false（加载失败）的也**静默跳过**（:362 `continue`，无日志）。成功句柄记入 `_loadedNativeHandles`（comparer 为 `OrdinalIgnoreCase`，:371-372）：一律以 `Path.GetFileNameWithoutExtension(file)` 为键（:365）；文件以 `.dll` 结尾（OrdinalIgnoreCase）时**额外**再以 `Path.GetFileName(file)`（带扩展名）为第二个键存同一句柄（:366-367），兼容 DllImport 名带/不带 `.dll` 后缀两种写法。 |

## 调用方式与生命周期

唯一典型调用序列就是 `Program.Main`（Program.cs:13-17）：

```csharp
Launcher.Initialize();   // 引导：程序集解析 + native 预加载
Launcher.Run(args);      // 记录日志 → Avalonia 主循环（不返回，直至退出）
```

生命周期约束（详细不变量见 pitfalls.md）：

1. `Initialize()` 必须先于任何 Avalonia/SkiaSharp/HarfBuzzSharp 类型的使用（JIT 时机即加载时机）。
2. `RegisterNativeResolversForAvalonia()` 必须晚于 `AppBuilder` 构造（`RunAvalonia` 内的调用点 Launcher.cs:62 是唯一正确位置）。
3. `AssemblyLoader` 各 `Initialize`/`RegisterNativeResolvers` 幂等（布尔守卫 + 锁），可重复调用无副作用。
4. `args` 原样透传：`Main(args)` → `Run(args)` → `RunAvalonia(args)` → `StartWithClassicDesktopLifetime(args)`，本模块不解析参数。

## 对外公开的数据结构

模块**不**定义任何公开数据结构/record/DTO。需要知道的内部配置数据（全在 AssemblyLoader.cs，均为 `private static readonly`）：

- `BootRequiredAssemblyFiles`（:23-31）：启动预加载清单——`Libraries/Serilog/Serilog.dll`、`Libraries/Serilog/Serilog.Sinks.Console.dll`、`Core/DigitalWorkstation.Core.Common.dll`、`Libraries/Avalonia/Avalonia.Base.dll`、`Avalonia.Controls.dll`、`Avalonia.dll`（相对 `AppContext.BaseDirectory`，大小写不敏感的文件系统上均能找到）。
- `BaseFolderPath`（:36-43）：搜索根目录 → 子目录递归深度的映射：`{根:0, core/:0, libraries/:1, modules/:0, runtimes/:2}`。
- `SourceArray`（:125）：`[".dll", ".exe"]`，`LoadAssembly` 尝试的扩展名。
- `NativeLibraryDir`（:338-342）：`runtimes/{win-x64|osx|linux-x64}/native/`，rid 由 `RuntimeInformation.IsOSPlatform` 三段判定（Windows→win-x64、OSX→osx、其余→linux-x64）。
