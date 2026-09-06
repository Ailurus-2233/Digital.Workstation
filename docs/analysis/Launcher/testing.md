# Launcher — 验证方式

## 本模块的测试在哪

**没有。** 仓库唯一的测试项目是 `UnitTest/Framework/`（`UnitTest/Framework/Framework.csproj`，仅含 `ShellLayoutStateResizeTests.cs`），不存在 Launcher 的单元测试项目。`Directory.Build.props:5` 与 `Directory.Build.targets:5` 的 import 条件显式排除 `\UnitTest\` 路径，说明测试项目不参与 `ManageDlls` 分类布局，也从侧面印证 Launcher 没有被测项目。

## 仓库测试约定（本模块的适用结论）

- 本仓库是纯桌面端（Avalonia）项目，**约定不做 UI 自动化测试验收**。
- 单元测试聚焦数据检测（数据读写、转换、校验、计算逻辑），框架 xUnit，集中于 `UnitTest/` 下按被测项目命名的目录。
- Launcher 的职责是进程引导与运行时解析——属于"启动即验证"代码：它要么让整个应用起来，要么起不来，没有可独立断言的数据逻辑。因此对本模块的验证方式就是**手动运行验收**。

## 怎么跑（手动验证）

### Debug（开发日常验证）

```
dotnet run --project Launcher/Launcher.csproj
```

- Debug 布局把所有 DLL 平铺到 `Output/Debug/`，`AssemblyLoader.IsDesignEnvironment()`（AssemblyLoader.cs:138-146）在 DEBUG 下恒 `true`，整个引导器禁用——**Debug 运行不覆盖 AssemblyLoader 的任何逻辑**，只验证 `Program.Main` → `Launcher.Run` → `WorkstationApplication` 的启动链与 Avalonia 配置（`BuildAvaloniaApp`，Launcher.cs:66-73）。
- 验收点：应用窗口（Workstation shell 主窗口）正常出现；日志（Serilog Console sink）出现 `Application startup.`（Launcher.cs:47）。
- Avalonia 设计器/预览器验证：在 IDE 中打开任一 axaml，预览器能渲染即说明 `Program.BuildAvaloniaApp()`（Program.cs:23）约定入口工作正常。

### Release（AssemblyLoader 逻辑的唯一验证途径）

```
dotnet build Launcher/Launcher.csproj -c Release
Output\Release\Launcher.exe          # 双击或命令行启动
```

- Release 构建触发 `Build/ManageDlls.targets`：DLL 分类进 `libraries/<分类>/`、Core/Modules 项目进 `core/`、`modules/`、`runtimes/` 只留 `linux-x64`/`osx`/`win-x64`。
- **只有这条路径会真正执行** `AssemblyLoader.Initialize()` 的预加载与解析器注册（AssemblyLoader.cs:62-87）、`PreloadNativeLibraries`（:349-369）与 `RegisterNativeResolversForAvalonia`（:323-333）。
- 验收点：
  1. `Output\Release\` 根目录只剩 `Launcher.exe` 等少数文件（DLL 已被 `ClearDllFiles` 清走）；
  2. 应用正常启动并显示主窗口——说明分类目录下的托管解析（`ResolveAssembly`）工作；
  3. 界面正常渲染文字与矢量图——说明 SkiaSharp/HarfBuzzSharp 的 native 库（`runtimes/<rid>/native/`）被 `PreloadNativeLibraries` 预加载且 P/Invoke 命中（若失败会在首次渲染时抛 `DllNotFoundException`）；
  4. 从**其他工作目录**用绝对路径启动 `Launcher.exe` 也正常——验证 `BaseDirectory = AppContext.BaseDirectory`（AssemblyLoader.cs:18）的路径锚定（注释 :15-17 明确这是设计目标）。

## 改完代码后的最小验证集

| 改动 | 必须手动验证 |
|---|---|
| 动 `AssemblyLoader` 任何方法 | Release 构建 + 启动 + 界面渲染（上述 1-3） |
| 动 `BootRequiredAssemblyFiles`/`BaseFolderPath`/`NativeLibraryDir` | 同上，另加从非程序目录启动（验收点 4） |
| 动 `Launcher.BuildAvaloniaApp`/`RunAvalonia` | Debug `dotnet run` 正常启动；IDE axaml 预览器正常渲染 |
| 动 `Program.Main` 结构 | 同上一行，且确认 `Main` 体内仍不直接引用 Avalonia 类型（见 pitfalls.md） |

## 如何"新增一个测试"

按仓库约定，若未来要为 Launcher 补数据逻辑测试（如 `NativeFilePatterns` 的平台映射），应在 `UnitTest/` 下新建目录与 `xUnit` 项目，命名空间/程序集名由 `Build/Base.props:44-47` 的 `_InUnitTest` 规则自动生成为 `DigitalWorkstation.UnitTest.<目录名>`；但当前仓库无此先例，本模块亦无可测的纯数据逻辑被拆出——**现状即约定：入口项目靠 `dotnet run` 手动验证**。
