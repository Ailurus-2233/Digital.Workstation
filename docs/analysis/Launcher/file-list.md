# Launcher — 文件结构与功能

目录树（相对 `Launcher/`，共 4 个文件，全部位于目录根部，无子目录）：

```
Launcher/
├── Launcher.csproj      项目文件
├── Program.cs           进程入口 + Avalonia 设计器入口
├── Launcher.cs          启动流程编排（引导顺序控制 + AppBuilder 构建）
└── AssemblyLoader.cs    自定义程序集/native 库加载器（本模块主体，438 行）
```

## Launcher.csproj

- **功能**：WinExe 入口项目定义。`<OutputType>WinExe</OutputType>`（:4）、`net10.0`（:7）、`ImplicitUsings`/`Nullable` enable（:5-6）、显式 `<AssemblyName>Launcher</AssemblyName>`（:8，覆盖 `Build/Base.props` 的命名规则），唯一 ProjectReference 指向 `..\Modules\Workstation\Workstation.csproj`（:12）。
- 不 import 任何 Build 脚本；`Build/Base.*`、`Build/ManageDlls.*` 经仓库根 `Directory.Build.props`/`Directory.Build.targets` 统一进入。

## Program.cs（24 行）

- **功能**：进程入口类 `DigitalWorkstation.Launcher.Program`（:7）。
- 关键成员：
  - `private static void Main(string[] args)`（:13-17）：`Launcher.Initialize()` → `Launcher.Run(args)`。方法体内不出现 Avalonia 类型，保证 JIT 本方法不需要加载 Avalonia。
  - `public static AppBuilder BuildAvaloniaApp()`（:23）：Avalonia 设计器/预览器约定入口，转发 `Launcher.BuildAvaloniaApp()`；XML 注释（:19-22）说明设计时 `AssemblyLoader.Initialize()` 自动禁用、程序集从输出根目录探测。

## Launcher.cs（73 行）

- **功能**：`public static class Launcher`（:16），编排"引导 → 日志 → Avalonia 主循环"的严格顺序。
- 关键成员：
  - `Initialize()`（:21-30）：依次 `AssemblyLoader.Initialize()`、`RegisterNativeResolvers()`、`PreloadNativeLibraries()`、`InitializeCore()`；注释（:24-26）说明此处只引用自身程序集、不触发 Avalonia/SkiaSharp 加载。
  - `InitializeCore()`（:32-35）：私有空方法，注释 `TODO 添加初始化逻辑`，是预留给核心框架初始化的扩展点。
  - `Run(string[] args)`（:44-49，`[STAThread]`）：`Logger.Information("Application startup.", nameof(Launcher))` 后调 `RunAvalonia`。
  - `RunAvalonia(string[] args)`（:56-64，`[MethodImpl(MethodImplOptions.NoInlining)]`）：`BuildAvaloniaApp()` → `AssemblyLoader.RegisterNativeResolversForAvalonia()` → `builder.StartWithClassicDesktopLifetime(args)`。
  - `BuildAvaloniaApp()`（:66-73）：`AppBuilder.Configure<WorkstationApplication>().UsePlatformDetect().WithInterFont().LogToTrace().WithDeveloperTools()`。

## AssemblyLoader.cs（438 行）

- **功能**：`public sealed class AssemblyLoader`（:12），Lazy 单例（:47-53）。两大职责：托管程序集解析与 native 库解析。
- 区域结构：
  - 字段区（:14-43）：`BaseDirectory`、`BootRequiredAssemblyFiles`、`BaseFolderPath`。
  - `#region Singleton`（:45-55）：Lazy 单例与私有构造。
  - `#region public API`（:57-117）：`Initialize`、`LoadAssembly`、`RefreshRuntimeEnvironmentPath`。
  - `#region Assembly Resolve fields`（:119-204）：锁/缓存/搜索路径字段；`IsDesignEnvironment`（:138，DEBUG 恒 true）；`PreloadBootAssemblies`（:148，用 `LoadFile`）；`InitializeSearchPath`（:165）与递归枚举 `GetAllSubDirectories`（:187，深度参数 default 4，实际由 `BaseFolderPath` 各项提供 0/1/2）。
  - `#region Resolve Assembly`（:206-299）：`ResolveAssembly`（:211）三级回退；`ResolveAssemblyFromCache`（:230）/`ResolveAssemblyFromLoaded`（:235）/`ResolveAssemblyFromSearchPaths`（:248）；`CacheNativeDirectory`（:278）与 `_nativeDirs`（:297）。
  - `#region Resolve Native Library`（:301-437）：`RegisterNativeResolvers`（:308）、`RegisterNativeResolversForAvalonia`（:323）、`NativeLibraryDir`（:338）、`PreloadNativeLibraries`（:349）、`_loadedNativeHandles`（:371）、`ResolveNativeLibrary`（:379，四级回退）、`TryLoadFromDir`（:416）、`NativeFilePatterns`（:428，按平台生成文件名候选）。

## 相关但不在本目录的文件（发布布局对偶）

- `Directory.Build.props` / `Directory.Build.targets`（仓库根）：统一 import Build 脚本。
- `Build/Base.props`：`BaseOutputPath=Output\<Configuration>\`，Release 下 Core→`core\`、Modules→`modules\`、其余（含 Launcher）→根。
- `Build/ManageDlls.props` / `Build/ManageDlls.targets`：Release 下项目引用不复制、NuGet 资产按包名首段分类进 `libraries\<分类>\`、输出根清理 DLL、`runtimes/` 只保留 `linux-x64`/`osx`/`win-x64`。
