# Launcher — 模块简述

## 模块做什么

`Launcher/` 是 Digital.Workstation 解决方案的**程序入口与运行时引导器**（WinExe，`net10.0`，程序集名 `Launcher`，Launcher.csproj:3-9）。它只做一件事：把"分类目录布局"的发布产物正确启动起来。具体职责有两个：

1. **自定义程序集/native 库解析**（`AssemblyLoader`，AssemblyLoader.cs:12）：Release 发布布局中 DLL 不堆在输出根目录，而是按用途分到 `core/`、`modules/`、`libraries/<包分类>/`、`runtimes/<rid>/native/` 等子目录，CLR 默认 probing 找不到。`AssemblyLoader` 负责预加载启动关键 DLL、注册 `AppDomain.AssemblyResolve` 兜底解析器、注册 `NativeLibrary.SetDllImportResolver` 并预加载所有 native 库。
2. **启动 Avalonia/Prism 应用**（`Launcher`，Launcher.cs:16）：指定 `WorkstationApplication`（Modules/Workstation）为 Avalonia Application，构建 `AppBuilder` 并以经典桌面生命周期启动。

## 核心设计逻辑

- **引导顺序即 JIT 顺序**：`Program.Main`（Program.cs:13-17）只调 `Launcher.Initialize()` 再 `Launcher.Run(args)`，两个方法体内都不出现 Avalonia/SkiaSharp 类型。`Initialize`（Launcher.cs:21-30）先 `AssemblyLoader.Initialize()`（预加载 + 注册 AssemblyResolve），再 `RegisterNativeResolvers()` + `PreloadNativeLibraries()`——注释明确"此处只引用 Launcher 自身程序集，不会触发 Avalonia/SkiaSharp 加载"（Launcher.cs:26）。真正的 Avalonia 启动被拆进独立的 `RunAvalonia` 并标注 `[MethodImpl(MethodImplOptions.NoInlining)]`（Launcher.cs:56-64），保证 Avalonia 类型的 JIT 推迟到引导程序集预加载、`AssemblyResolve` 注册完成之后；若把 `builder.StartWithClassicDesktopLifetime` 直接写回 `Run`，JIT 可能在 `AssemblyLoader.Initialize()` 之前就解析 `AppBuilder` 类型而失败。同一方法内还有一个时序对称的约束：`AssemblyLoader.RegisterNativeResolversForAvalonia()` 排在 `BuildAvaloniaApp()` **之后**（Launcher.cs:59-62）——`AppBuilder` 构造完毕意味着 Avalonia 程序集已全部加载，此刻 `typeof(SkiaSharp.SKImageInfo)`/`typeof(HarfBuzzSharp.Blob)` 才安全；提前调用会因 `typeof` 引用触发程序集加载链在引导未完成时执行而崩溃（注释 AssemblyLoader.cs:319-322）。
- **设计时/DEBUG 自动禁用**：`AssemblyLoader.IsDesignEnvironment()`（AssemblyLoader.cs:138-146）的实现就是 `#if DEBUG → return true`，其后的 `return false` 被 `#pragma warning disable/restore CS0162`（:143、:145）包裹以抑制"检测到不可达代码"警告。Debug 构建的输出是平铺布局（所有 DLL 直接进 `Output/Debug/` 根目录），默认 probing 即可工作，所以整个 `Initialize()` 在 DEBUG 下只置 `_isInitialized = true` 便返回（AssemblyLoader.cs:69-73）——预加载、`InitializeSearchPath`、`RefreshRuntimeEnvironmentPath`、`AssemblyResolve` 注册四步全部跳过。`Program.BuildAvaloniaApp()`（Program.cs:23）是 Avalonia 设计器/预览器约定入口，预览器走 DEBUG 构建，因此预览时不会触碰分类目录逻辑。
- **预加载用 LoadFile 防递归**：`PreloadBootAssemblies`（AssemblyLoader.cs:148-160）用 `Assembly.LoadFile`（不是 `LoadFrom`）加载 `BootRequiredAssemblyFiles`（Serilog 两个 + `Core/DigitalWorkstation.Core.Common.dll` + Avalonia 三个），注释注明避免 probing/AssemblyResolve 递归导致 `StackOverflowException`（AssemblyLoader.cs:154），加载结果按简单名塞进 `_resolvedCache` 供后续解析命中。
- **目录分类约定与 Build 脚本对偶**：运行时搜索路径 `BaseFolderPath`（AssemblyLoader.cs:36-43）是 `{输出根, core/, libraries/, modules/, runtimes/}`（附各自子目录递归深度：core/modules 0 级、libraries 1 级、runtimes 2 级），与 `Build/ManageDlls.targets` 在 Release 下生成的布局一一对应（详见 reference.md 发布布局节）。改任何一边必须同步另一边。
- **native 库"预加载常驻 + 按名命中"策略**：`PreloadNativeLibraries`（AssemblyLoader.cs:349-369）在 Avalonia 加载前扫描 `runtimes/<rid>/native/` 和 `libraries/` 下所有名为 `native` 的目录，把每个 `.dylib/.so/.dll` 用 `NativeLibrary.TryLoad` 加载并按文件名（不含扩展名，dll 另存带扩展名键）记入 `_loadedNativeHandles`；之后任何 P/Invoke 经 `ResolveNativeLibrary`（AssemblyLoader.cs:379-414）第一级就直接命中已加载 handle，天然绕过了运行时 RID 解析。
- **Avalonia 相关 native 解析器延迟注册**：`RegisterNativeResolversForAvalonia`（AssemblyLoader.cs:323-333）为 SkiaSharp、HarfBuzzSharp、Avalonia.Native 三个第三方程序集注册 resolver，但必须在 `AppBuilder` 构造完成后调用（Launcher.cs:60-62），因为 `typeof(SkiaSharp.SKImageInfo)` 这类引用本身会触发程序集加载；`AvaloniaNativePlatform` 是 internal，故按名字 `"Avalonia.Native"` 从已加载程序集列表里找（AssemblyLoader.cs:328-332）。

## 状态流转

```
Program.Main(args)                                     [Program.cs:13]
  → Launcher.Initialize()                              [Launcher.cs:21]
      → AssemblyLoader.Initialize()                    [AssemblyLoader.cs:62]
          · lock(InitLock)；_isInitialized 守卫（幂等）
          · DEBUG：IsDesignEnvironment()==true → 置标志直接返回（无后续步骤）
          · Release：PreloadBootAssemblies（LoadFile × 6 个启动 DLL → _resolvedCache）
          · InitializeSearchPath（_searchPaths = BaseFolderPath 键 + 按深度限制的子目录）
          · RefreshRuntimeEnvironmentPath（把 _searchPaths 追加进进程 PATH 环境变量）
          · AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly
      → AssemblyLoader.RegisterNativeResolvers()       [AssemblyLoader.cs:308]
          · SetDllImportResolver(Launcher 程序集, ResolveNativeLibrary)
      → AssemblyLoader.PreloadNativeLibraries()        [AssemblyLoader.cs:349]
          · TryLoad 所有 native 库 → _loadedNativeHandles[文件名] = handle
      → InitializeCore()                               [Launcher.cs:32]（当前为空，TODO）
  → Launcher.Run(args)                                 [Launcher.cs:45]
      · Logger.Information("Application startup.", nameof(Launcher))（Core.Common）
      → RunAvalonia(args)                              [Launcher.cs:57，NoInlining]
          → BuildAvaloniaApp()                         [Launcher.cs:66]
              AppBuilder.Configure<WorkstationApplication>()
                .UsePlatformDetect().WithInterFont().LogToTrace().WithDeveloperTools()
          → RegisterNativeResolversForAvalonia()       [AssemblyLoader.cs:323]
          → builder.StartWithClassicDesktopLifetime(args)   ← 进入 Avalonia 主循环
```

运行时兜底路径（启动后惰性触发）：

1. **托管程序集解析**：CLR 找不到某程序集 → `AssemblyResolve` 事件 → `ResolveAssembly`（AssemblyLoader.cs:211-228）：先解析 `AssemblyName`，以 `.resources` 结尾的卫星程序集名直接返回 `null`（走默认卫星 probing，:215）→ ① `ResolveAssemblyFromCache` 查 `_resolvedCache` → ② `ResolveAssemblyFromLoaded` 遍历 `AppDomain.GetAssemblies()` 按 `OrdinalIgnoreCase` 匹配已加载程序集，命中回填 `_resolvedCache`（:241）→ ③ `ResolveAssemblyFromSearchPaths`（:248-272）：先按 `assemblyName.Split('.')[0]` 找名称包含该段的搜索目录（如 `SkiaSharp` → `libraries/SkiaSharp`）尝试 `LoadAssembly`，失败则遍历全部 `_searchPaths`；两处命中同样写回 `_resolvedCache`（:260、:267）——即任何一级解析成功都会进缓存。`LoadAssembly`（:92-102）对每个候选目录依次尝试 `{name}.dll` 与 `{name}.exe` 两个扩展名（`SourceArray`，:125），用 `Assembly.LoadFrom` 加载。全部落空返回 `null`（CLR 抛 `FileNotFoundException`）。
2. **native 库解析**：P/Invoke 触发 → `ResolveNativeLibrary`：① `_loadedNativeHandles` 按名命中 → ② `_nativeDirs`（程序集解析成功时由 `CacheNativeDirectory` 记录的"同包附带 native 目录"）→ ③ `runtimes/<rid>/native/` → ④ 在 `libraries/` 全树按平台文件名模式搜索但限定路径含 `/runtimes/<当前rid>/`（避免误加载其他平台二进制）；全部落空返回 `IntPtr.Zero`，交还默认解析。

副作用：进程级——修改 `PATH` 环境变量（`RefreshRuntimeEnvironmentPath`）、向 `AppDomain` 挂事件、向 `NativeLibrary` 注册 resolver、预加载 native 句柄常驻；模块自身状态只有 `_resolvedCache`/`_searchPaths`/`_nativeDirs`/`_loadedNativeHandles` 四个并发容器与 `_isInitialized`/`_nativeResolversRegistered` 两个布尔守卫。不读写配置文件、不开线程。

## 常见修改场景

1. **新增一个启动早期就要用的程序集**（如在 `InitializeCore` 里要用某个库）：把它的相对路径加进 `BootRequiredAssemblyFiles`（AssemblyLoader.cs:23-31），保证 `AssemblyResolve` 注册前已被 `LoadFile` 预加载；否则该程序集的依赖解析可能发生在引导完成前而失败。
2. **接入一个新的含 native 资产的第三方包**（如新图像库）：若它在 Avalonia 加载之后才使用，在 `RegisterNativeResolversForAvalonia`（AssemblyLoader.cs:323-333）里加一行 `NativeLibrary.SetDllImportResolver(typeof(新包某类型).Assembly, ResolveNativeLibrary)`；若 DllImport 名与磁盘文件名不一致，检查 `NativeFilePatterns`（AssemblyLoader.cs:428-435）是否覆盖该命名（Windows `name.dll`、macOS `libname.dylib`、Linux `libname.so`）。
3. **更换/包装启动的应用类型**：改 `Launcher.BuildAvaloniaApp()`（Launcher.cs:66-73）中 `AppBuilder.Configure<WorkstationApplication>()` 的泛型实参与链式配置；注意保持 `RunAvalonia` 的 `NoInlining` 拆分结构不变。
4. **调整 Release 发布目录分类**（如新增 `plugins/` 目录）：改 `Build/ManageDlls.targets` 的分类逻辑，同时必须把新目录加进 `BaseFolderPath`（AssemblyLoader.cs:36-43）并给出合适的子目录递归深度，否则运行时解析找不到。
5. **处理启动参数**：`args` 从 `Program.Main` 原样透传到 `StartWithClassicDesktopLifetime(args)`（Launcher.cs:63）；要拦截参数应在 `Launcher.Run`（Launcher.cs:45-49）里 `Logger.Information` 之后、进入 `RunAvalonia` 之前处理，避免在 `Main` 里直接引用 Avalonia 类型。
