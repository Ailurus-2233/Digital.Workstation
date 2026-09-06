# Launcher — 异常与排查

本模块**没有任何 try/catch**，不抛自定义异常；错误策略是"可预期的缺失静默跳过，真正的缺失交给 CLR 默认异常"。逐类列出：

## 1. 启动 DLL 缺失 / `System.IO.FileNotFoundException`（托管程序集）

- **触发条件**：`BootRequiredAssemblyFiles`（AssemblyLoader.cs:23-31）中某项在磁盘不存在，或运行时某程序集经 `ResolveAssembly`（:211-228）三级回退后仍返回 `null`。
- **行为差异**：`PreloadBootAssemblies`（:148-160）对缺失文件 `continue` **静默跳过**；但后续一旦有代码引用该程序集的类型，CLR 触发 `AssemblyResolve` 也找不到时，抛 `FileNotFoundException`（或由调用栈包装为 `TypeLoadException`/`FileLoadException`）。
- **抛出位置**：不是本模块代码抛出，而是 CLR 在 JIT 引用方方法时抛出——典型现场是 `Launcher.RunAvalonia`（Launcher.cs:57）或 Workstation 侧代码首次触碰某类型时。
- **排查**：
  1. 确认是 Debug 还是 Release 启动。Debug 平铺布局下引导器整体禁用（`IsDesignEnvironment`，AssemblyLoader.cs:138-146），找不到 DLL 说明构建输出本身不全；
  2. Release 下检查输出根目录结构是否符合 `Build/ManageDlls.targets` 约定：`core/`、`modules/`、`libraries/<分类>/` 是否存在、目标 DLL 是否落在与 `NuGetPackageId.Split('.')[0]` 对应的分类目录；
  3. 检查 `ResolveAssemblyFromSearchPaths` 的优先匹配（:253-254）：`assemblyName.Split('.')[0]` 命中的第一个"名称包含该段"的目录里若没有目标 DLL，会**继续**遍历全部 `_searchPaths`（:265-269），所以"放错分类目录"通常仍能加载，只是失去优先路径；
  4. `.resources` 卫星程序集在 :215 被显式跳过返回 `null`——这是有意为之（走默认卫星 probing），不要误判为 bug。

## 2. native 库加载失败 / `DllNotFoundException`、`EntryPointNotFoundException`

- **触发条件**：P/Invoke 目标库既不在 `_loadedNativeHandles`（预加载未覆盖）、`ResolveNativeLibrary`（:379-414）四级回退全部落空（返回 `IntPtr.Zero` 交还默认解析），默认 probing 也找不到。
- **抛出位置**：CLR 在实际调用 P/Invoke 方法时抛出，非本模块代码。
- **设计上的防错层**：
  - `PreloadNativeLibraries`（:349-369）对单个文件 `NativeLibrary.TryLoad` 失败 **静默跳过**（:362 `continue`）——这是最常见的"静默失败点"：依赖库缺失（如 VC++ 运行库）导致 `TryLoad` 失败时，启动日志里没有任何记录，直到第一次真正 P/Invoke 才炸；
  - `RegisterNativeResolversForAvalonia` 若早于 Avalonia 程序集加载完成被调用，`typeof(SkiaSharp.SKImageInfo)` 会触发加载链在引导未完成时执行——表现为启动早期崩溃（注释见 :319-322 与 Launcher.cs:60-62）。
- **排查**：
  1. 确认 `runtimes/{win-x64|osx|linux-x64}/native/` 下对应平台的 `.dll/.dylib/.so` 存在（`NativeLibraryDir`，:338-342；Release 构建 `ClearDllFiles` 只保留这三个 rid）；
  2. 用进程监视工具看实际探测路径；`RefreshRuntimeEnvironmentPath`（:107-115）已把 `_searchPaths` 追加进 `PATH`，若 DLL 的**间接依赖**（非 P/Invoke 直接目标的依赖库）缺失，看 `PATH` 是否包含对应目录；
  3. DllImport 名与文件名不匹配时对照 `NativeFilePatterns`（:428-435）：Windows 只认 `{name}.dll` 与无扩展名 `name`；macOS `lib{name}.dylib`/`{name}.dylib`；Linux `lib{name}.so`/`{name}.so`；
  4. 兜底搜索（:405-411）限定文件路径必须含 `/runtimes/<当前rid>/`——若 native 资产被复制到不含该段的目录（如被 `ManageDlls` 平铺进 `libraries/<分类>/` 根），兜底搜索找不到它，只能依赖 `PreloadNativeLibraries` 或 `_nativeDirs` 命中。

## 3. `StackOverflowException`（历史风险，已被设计消除）

- **来源**：`AssemblyResolve` 处理器内部若再触发程序集解析（例如用 `LoadFrom` 走 probing 递归进事件），会无限递归。`PreloadBootAssemblies` 用 `Assembly.LoadFile` 而非 `LoadFrom` 正是为此（注释 :154）。`LoadFrom` 仅用于 `LoadAssembly`（:101）即事件链路的末端，目标文件已定位、不再递归。
- **排查**：若新增代码在 `ResolveAssembly` 调用链里引用了"尚未加载的程序集的类型"，就会重新引入递归——修改解析器时保持处理器本体只依赖 BCL。

## 4. 设计器/预览器异常

- **触发条件**：Avalonia 预览器宿主调用 `Program.BuildAvaloniaApp()`（Program.cs:23）。DEBUG 构建下 `AssemblyLoader.Initialize()` 空转（:69-73），程序集靠输出根平铺探测；若有人"修复" `IsDesignEnvironment` 让 DEBUG 也执行引导逻辑，预览器会因输出目录结构与 `BootRequiredAssemblyFiles` 相对路径（`Libraries/...`、`Core/...`）不匹配而全部静默跳过——预览仍可能工作（默认 probing 兜底），但 `PATH` 被改写、解析器注册顺序等副作用会污染预览宿主进程。
- **排查**：预览器找不到程序集时先看输出根是否有平铺 DLL（构建是否成功），再看是否误改了 `#if DEBUG` 分支。

## 5. 重复初始化

- 不是异常：`Initialize`（:64-66）与 `RegisterNativeResolvers`（:310-315）都有布尔守卫 + `InitLock`，重复调用直接返回。若观察到"第二次调用没生效"，这是**设计行为**而非 bug；要真正重置只能重启进程（无反注册 API）。

## 错误处理路径总览

```
缺失/失败一律不向调用方返回错误码：
  PreloadBootAssemblies     → 文件缺失 continue（静默）
  LoadAssembly              → 目录/文件不存在返回 null（由 ResolveAssembly 链消化）
  PreloadNativeLibraries    → TryLoad 失败 continue（静默）
  ResolveNativeLibrary      → 全部落空返回 IntPtr.Zero（交还 CLR 默认解析）
  ResolveAssembly           → 全部落空返回 null（CLR 抛 FileNotFoundException）
最终显性异常全部是 CLR 运行时异常（FileNotFoundException / DllNotFoundException /
TypeLoadException / EntryPointNotFoundException），抛出点在"首次使用该类型的方法"，
不在本模块代码行内。
```

日志线索：启动成功的第一条日志是 `Launcher.Run` 的 `"Application startup."`（Launcher.cs:47，经 `DigitalWorkstation.Core.Common.Logger`）。**看不到这条日志 = 崩在引导阶段**（AssemblyLoader 三个静态方法内）；**看得到但随后 DllNotFound = Avalonia 启动后 native 解析未命中**，按第 2 节排查。
