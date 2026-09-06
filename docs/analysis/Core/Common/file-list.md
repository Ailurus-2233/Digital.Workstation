# Common — 文件结构与功能

模块目录：`Core/Common/`（相对源仓库根 `D:/Sources/Person/Digital.Workstation`）。

## 目录树

```
Core/Common/
├── Common.csproj      # 项目定义：net10.0，4 个 NuGet 包 + 1 个项目引用
├── Logger.cs          # Serilog 静态日志门面
├── IoC.cs             # Prism 容器静态访问器
├── obj/               # 构建中间产物（非源码，勿改）
│   ├── Debug/         # 含 Common.GlobalUsings.g.cs（Prism global using 的证据）等生成文件
│   ├── Release/
│   └── *.json/*.props/*.targets/*.cache
└── Output/            # 历史遗留空输出目录（构建输出已由 Build/Base.props 改到仓库根 Output/）
```

## 逐文件说明

### Common.csproj

项目定义文件。关键内容：
- `net10.0` + `ImplicitUsings` + `Nullable enable`（第 4-7 行）；
- NuGet：`Prism.Avalonia` 9.0.537.11130、`Prism.DryIoc.Avalonia` 9.0.537.11130、`Serilog` 4.4.0、`Serilog.Sinks.Console` 6.1.1（第 10-13 行）；
- `ProjectReference` 指向 `..\Abstractions\Abstractions.csproj`（第 17 行）；
- 不显式设置程序集名/命名空间/输出路径——由仓库级 `Build/Base.props` 经 `Directory.Build.props` 导入统一约定（`_InCore` 分支 → `DigitalWorkstation.Core.Common`）。

### Logger.cs

唯一类型 `DigitalWorkstation.Core.Common.Logger`。Serilog 日志的静态门面：
- `#region Singleton`（第 11-50 行）：`Lazy<Logger> SingleInstance`、私有构造函数构建 Serilog `ILogger`（Release 配 `MinimumLevel.Information()`，`#if DEBUG` 重建为 `Verbose()`；均含 `Override("Microsoft", Warning)`、`Enrich.FromLogContext()`、Console sink 模板 `[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}`）；
- `#region Public API`（第 52-177 行）：私有出口属性 `Log` + 8 个公开静态方法——`Verbose`/`Debug`/`Information`/`Warning`/`Error`/`Fatal` 及 `Error(Exception?, ...)`/`Fatal(Exception?, ...)` 两个重载，全部以固定模板 `"[{Sender}] {Message}"` 转发。

### IoC.cs

唯一类型 `DigitalWorkstation.Core.Common.IoC`。Prism DI 容器的静态访问器：
- `#region Singleton Implementation`（第 9-28 行）：`Lazy<IoC> InnerInstance` + 私有构造函数；
- `#region 容器注册`（第 30-76 行）：实例字段 `_registry`/`_provider`（`null!` 初始化）、公开静态属性 `Registry`/`Provider`（`private set`）、实例闸门属性 `IsInitialized`、一次性静态方法 `Initialize(IContainerRegistry, IContainerProvider)`（重复调用抛 `InvalidOperationException`）。
- 全文件无 `using` 指令：Prism 命名空间由包传递注入的 global using 提供（见 `obj/Debug/Common.GlobalUsings.g.cs`）。

### obj/ 与 Output/

- `obj/`：MSBuild/NuGet 中间产物。其中 `obj/Debug/Common.GlobalUsings.g.cs` 是理解 IoC.cs 为何不需要 using 的关键证据；`project.assets.json` 记录包解析结果。均自动生成，不属于源码。
- `Output/Debug/`：5 个月前的空目录遗留；当前构建输出由 `Build/Base.props` 重定向到仓库根 `Output/$(Configuration)/`，此目录已不被使用。
