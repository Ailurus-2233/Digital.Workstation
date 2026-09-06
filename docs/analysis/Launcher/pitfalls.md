# Launcher — 不变量与陷阱

## 隐含不变量

1. **JIT 顺序不变量——入口方法不许直接引用 Avalonia 类型**。`Program.Main`（Program.cs:13-17）体内只有 `Launcher.Initialize()`/`Launcher.Run(args)`；Avalonia 类型全部隔离在 `[MethodImpl(MethodImplOptions.NoInlining)]` 的 `RunAvalonia`（Launcher.cs:56-64）与 `BuildAvaloniaApp`（:66）里。CLR 按方法 JIT：只要 `Main`/`Run` 体内不出现 Avalonia/SkiaSharp 类型，它们的类型解析就推迟到 `AssemblyLoader.Initialize()` 完成之后。`RunAvalonia` 的 XML 注释（Launcher.cs:51-55）明确说明这一点。**违反方式**：在 `Run` 里"顺手"加一句引用 `AppBuilder` 的日志/判空，`NoInlining` 拆分即失效。
2. **native resolver 注册时机分两段**：`RegisterNativeResolvers`（AssemblyLoader.cs:308，只引用自身程序集）必须在 Avalonia 加载**前**调（`Launcher.Initialize`，Launcher.cs:27）；`RegisterNativeResolversForAvalonia`（:323，含 `typeof(SkiaSharp.SKImageInfo)` 等）必须在 Avalonia 加载**后**调（`RunAvalonia` 中 `AppBuilder` 构造完之后，Launcher.cs:62）。两次调用的位置不可对调、不可合并。
3. **单例与幂等守卫**：`AssemblyLoader` 是 `Lazy<>` 单例（:47），私有构造；`Initialize`/`RegisterNativeResolvers` 均 `lock(InitLock)` + 布尔守卫（:64-66、:310-315）。没有对应的反初始化/反注册 API——状态一次写入、进程内常驻。
4. **路径锚定 `AppContext.BaseDirectory`**：所有相对路径基于 `BaseDirectory`（:18）而非当前工作目录，注释（:15-17）明确这是为了支持从任意工作目录启动。新增路径逻辑必须沿用 `BaseDirectory`，不许用 `Environment.CurrentDirectory`。
5. **`_searchPaths` 的唯一写入时机**：`InitializeSearchPath`（:165-182）在 `Initialize` 内执行一次；`RefreshRuntimeEnvironmentPath`（:107-115）读它追加 `PATH`。单独调用 `RefreshRuntimeEnvironmentPath` 而未先 `Initialize` 只会拿到空列表——两个 public API 存在隐式调用顺序。
6. **rid 集合硬编码三平台**：`NativeLibraryDir`（:338-342）与兜底搜索（:403-404）只认 `win-x64`/`osx`/`linux-x64`，与 `Build/ManageDlls.targets` `ClearDllFiles`（:57-59）保留的三个 rid 目录严格对偶。在 arm64 Windows 上运行会命中 `win-x64` 目录（依赖 x64 模拟），这是当前布局的隐含假设。

## 易错改法

1. **"修复" `IsDesignEnvironment`**：其实现是 `#if DEBUG return true`（:138-146）——即**整个 DEBUG 配置**（不只是设计器）都禁用引导器，方法名的"Design"表述偏窄。若改成运行时探测"是否预览器进程"，Debug 平铺布局下会执行预加载/解析器注册/`PATH` 改写：相对路径 `Libraries/...`、`Core/...`（`BootRequiredAssemblyFiles`，:23-31）在 Debug 输出根下不存在，全部静默 `continue`（:153），看似无害，但 `AssemblyResolve` 与 PATH 副作用会改变 Debug 调试时的加载行为，且污染预览器宿主进程。要支持"DEBUG 但想测引导器"应加显式开关而非改该方法语义。
2. **`PreloadBootAssemblies` 改用 `LoadFrom`**：注释（:154）明确 `LoadFile` 是为避免 probing/AssemblyResolve 递归（`StackOverflowException`）。`LoadFrom` 会走 probing 上下文，引导早期依赖解析可能重入未完全初始化的解析器。
3. **在 `ResolveAssembly` 链里引用第三方类型**：解析器（:211-272）本体只依赖 BCL。若在其中 new 某个 NuGet 类型或调用其它模块方法，会递归触发 `AssemblyResolve`。
4. **调整 `BaseFolderPath` 的深度值**：`libraries/:1`（:40）只枚举一层子目录（包分类目录），`runtimes/:2`（:42）两层（rid → native）。加深 `libraries` 的深度会把每个包目录下的全部嵌套目录（含 `runtimes` 子树）都塞进搜索路径与 `PATH`，显著放大误匹配面（同名 DLL 多平台版本并存时 `LoadAssembly` 可能挑到错误平台的文件——`LoadAssembly` 不做架构校验，`Assembly.LoadFrom` 对错误架构抛 `BadImageFormatException`）。
5. **`ResolveAssemblyFromSearchPaths` 的"优先目录"启发式**：`assemblyName.Split('.')[0]` + `Contains`（:253-254）。给包分类目录改名时若让两个分类目录名互为子串（如 `Avalonia` 与 `AvaloniaExtra`），`FirstOrDefault` 命中的目录取决于 `_searchPaths` 顺序而非精确匹配——行为仍正确（失败会继续全表遍历，:265-269），但排查"为什么先探测了别的目录"时会很迷惑。
6. **静默失败面**：`PreloadBootAssemblies` 缺文件 `continue`（:153）、`PreloadNativeLibraries` 的 `TryLoad` 失败 `continue`（:362）、`CacheNativeDirectory` 找不到直接返回（:284）——三处都无日志。启动失败的第一条可见线索只能是 CLR 异常 + `Launcher.Run` 的 `"Application startup."` 日志是否出现（见 error.md 第 5 节）。
7. **`_loadedNativeHandles` 双键约定**：dll 同时按"去扩展名"和"带扩展名"两键注册（:365-367），dylib/so 只按去扩展名。若新增平台/命名规则，需保持 `NativeFilePatterns`（:428-435）与此处键约定一致，否则 `ResolveNativeLibrary` 第一级命中失效。

## 历史踩坑（代码内证据）

- **Launcher.cs:51-55 注释**：`RunAvalonia` 独立成方法并 `NoInlining`，是踩过"Avalonia 类型 JIT 早于引导"的坑后留下的结构性防御。
- **Launcher.cs:24-26 注释**：`RegisterNativeResolvers` 位置被特意注明"只引用 Launcher 自身程序集，不会触发 Avalonia/SkiaSharp 加载"——说明曾因引用第三方类型导致过早加载。
- **AssemblyLoader.cs:319-322 注释**：`RegisterNativeResolversForAvalonia` 必须在 `AppBuilder` 构造后调用，"否则 typeof 引用会触发程序集加载而崩溃"。
- **AssemblyLoader.cs:327-328 注释**：`AvaloniaNativePlatform` 是 internal 无法 `typeof`，只能按程序集名 `"Avalonia.Native"` 从 `AppDomain.GetAssemblies()` 找——且找到与否不保证（`FirstOrDefault` + null 检查 :329-332，macOS 之外该程序集可能尚未加载，静默跳过）。
- **AssemblyLoader.cs:154 注释**：`LoadFile` vs `LoadFrom` 的递归陷阱。
- **Launcher.cs:34**：`InitializeCore()` 是 `TODO` 空方法——核心框架初始化扩展点尚未使用，往这里加逻辑时注意它执行于 native 预加载之后、Avalonia 启动之前。
