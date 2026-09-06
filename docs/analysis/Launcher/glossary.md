# Launcher — 术语表

| 术语 | 定义 | 首次出现位置 |
|---|---|---|
| **Launcher（启动器）** | 本模块/入口程序集本身（`AssemblyName=Launcher`，Launcher.csproj:8）。与通用"launcher"不同：它不仅启动进程，还承担 Release 分类布局下的程序集与 native 库解析职责。 | Launcher.cs:8-15 类注释 |
| **引导（Bootstrap）** | `Launcher.Initialize()`（Launcher.cs:21-30）执行的三步：程序集解析初始化 → 自身 native resolver 注册 → native 库预加载。发生在任何 Avalonia 类型被 JIT 之前。 | Launcher.cs:18-30 |
| **分类目录布局** | Release 发布产物目录结构：输出根 + `core/`（Core 项目 DLL）+ `modules/`（Modules 项目 DLL）+ `libraries/<包名首段>/`（NuGet 运行期资产，按 `NuGetPackageId.Split('.')[0]` 分类）+ `runtimes/<rid>/native/`（native 资产）。由 `Build/ManageDlls.targets` 生成，`AssemblyLoader.BaseFolderPath`（AssemblyLoader.cs:36-43）消费。 | AssemblyLoader.cs:33-43；Build/ManageDlls.targets:2-37 |
| **BootRequiredAssemblyFiles（启动必需程序集）** | 启动早期必须先用 `Assembly.LoadFile` 预加载的 6 个 DLL 相对路径清单（Serilog×2、Core.Common、Avalonia×3）。预加载是为了让 `AssemblyResolve` 注册前的早期解析不递归、注册后立即有缓存命中。 | AssemblyLoader.cs:20-31 |
| **AssemblyResolve（动态解析）** | `AppDomain.CurrentDomain.AssemblyResolve` 事件处理器 `ResolveAssembly`（AssemblyLoader.cs:211-228）：CLR 默认 probing 失败后的兜底，三级回退 = `_resolvedCache` → 已加载程序集 → `_searchPaths` 文件搜索。 | AssemblyLoader.cs:83 注册、:206-299 实现 |
| **DllImportResolver** | `NativeLibrary.SetDllImportResolver` 注册的回调 `ResolveNativeLibrary`（AssemblyLoader.cs:379-414）：P/Invoke 默认搜索失败后的兜底，四级回退 = `_loadedNativeHandles` → `_nativeDirs` → `runtimes/<rid>/native/` → `libraries/` 限定 rid 全树搜索。 | AssemblyLoader.cs:303-333 注册、:374-414 实现 |
| **预加载常驻（Preload）** | `PreloadNativeLibraries`（AssemblyLoader.cs:349-369）策略：启动时把所有 native 库 `NativeLibrary.TryLoad` 一遍并把句柄按文件名缓存，后续 P/Invoke 按 DllImport 名直接命中已加载句柄，绕过运行时 RID 解析。 | AssemblyLoader.cs:344-369 |
| **rid（运行时标识）** | 本模块只认三个：`win-x64`/`osx`/`linux-x64`（`NativeLibraryDir`，AssemblyLoader.cs:338-342；`ResolveNativeLibrary` 兜底 :403-404）。与 `Build/ManageDlls.targets` `ClearDllFiles` 保留的三个目录一一对应。注意 macOS 段就叫 `osx` 而非 `osx-arm64`。 | AssemblyLoader.cs:335-342 |
| **设计时（Design Environment）** | `IsDesignEnvironment()`（AssemblyLoader.cs:138-146）。**本模块的"设计时" = DEBUG 编译配置**（`#if DEBUG` 恒 `true`），不是运行时探测的预览器进程。效果：DEBUG 下 `AssemblyLoader.Initialize()` 空转，靠输出根平铺 DLL 的默认 probing；Release 下才真正执行引导。 | AssemblyLoader.cs:135-146；Program.cs:19-22 注释 |
| **`_nativeDirs`（同包 native 目录缓存）** | 程序集简单名 → 该程序集 NuGet 包附带的 native 资产目录。在托管程序集解析成功前由 `CacheNativeDirectory`（AssemblyLoader.cs:278-295）顺带记录，供 `ResolveNativeLibrary` 第二级回退按"调用方程序集反查同包 native 目录"。 | AssemblyLoader.cs:274-297 |
| **`_loadedNativeHandles`** | native 文件名（去扩展名；`.dll` 额外带扩展名键，比较器 `OrdinalIgnoreCase`）→ 已加载句柄的并发字典。`ResolveNativeLibrary` 第一级命中源。 | AssemblyLoader.cs:371-372 |
| **`NativeFilePatterns`** | 按平台把 DllImport 名映射为磁盘文件名候选：Windows `{name}.dll`/`{name}`；macOS `lib{name}.dylib`/`{name}.dylib`；Linux `lib{name}.so`/`{name}.so`。 | AssemblyLoader.cs:428-435 |
| **WorkstationApplication** | 本模块启动的 Avalonia Application 类型（来自 Modules/Workstation），`AppBuilder.Configure<WorkstationApplication>()`（Launcher.cs:68）。不是本模块定义，见 docs/analysis/Modules/Workstation/。 | Launcher.cs:4、:68 |
| **`StartWithClassicDesktopLifetime`** | Avalonia 桌面生命周期启动扩展方法：`RunAvalonia` 的最后一步（Launcher.cs:63），进入主循环后不返回直至退出。`args` 从 `Main` 原样透传到此。 | Launcher.cs:63 |
| **`InitializeCore`** | `Launcher` 的私有空方法（Launcher.cs:32-35），注释 `TODO 添加初始化逻辑`——预留给"核心框架初始化"的扩展点，执行时机在 native 预加载之后、Avalonia 启动之前。 | Launcher.cs:32-35 |
| **`BuildAvaloniaApp`（双份）** | Avalonia 设计器/预览器的约定入口签名 `public static AppBuilder BuildAvaloniaApp()`。本模块有两处：`Program.BuildAvaloniaApp()`（Program.cs:23，预览器反射调用的那个）转发 `Launcher.BuildAvaloniaApp()`（Launcher.cs:66，实际配置链所在）。 | Program.cs:19-23；Launcher.cs:66-73 |

## 与同名通用概念的区别

- **"搜索路径"**：本模块 `_searchPaths`（AssemblyLoader.cs:129）指**托管程序集**的搜索目录列表，由 `BaseFolderPath` + 限深子目录枚举构成；它与进程 `PATH` 环境变量的关系是单向的——`RefreshRuntimeEnvironmentPath`（:107-115）把 `_searchPaths` 追加进 `PATH` 供 **native 间接依赖**的默认搜索使用，反向不成立（`PATH` 里的目录不会进 `_searchPaths`）。
- **"缓存"**：`_resolvedCache` 缓存的是 `Assembly` 对象（托管），`_loadedNativeHandles` 缓存的是 `IntPtr` 句柄（native），`_nativeDirs` 缓存的是目录路径字符串（native 的中间索引）——三个"缓存"层级与内容都不同，勿混用。
- **"禁用"**：`IsDesignEnvironment` 的"设计时禁用"禁用的是 `AssemblyLoader.Initialize()` 的全部四个步骤（:69-73），但 `Launcher.Initialize()` 里的 `RegisterNativeResolvers`/`PreloadNativeLibraries` 并不受它控制（DEBUG 下这两个仍会执行——只是 DEBUG 平铺布局下 native 预加载目录 `runtimes/<rid>/native/` 同样存在，行为无害）。
