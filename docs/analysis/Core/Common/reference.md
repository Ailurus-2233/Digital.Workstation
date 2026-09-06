# Common — 模块关系链

## 依赖关系（本模块 → 外部）

来自 `Core/Common/Common.csproj`：

### NuGet 包

| 依赖 | 版本 | 用途 |
|---|---|---|
| `Prism.Avalonia` | 9.0.537.11130 | 提供 `Prism.Ioc.IContainerRegistry`/`Prism.Ioc.IContainerProvider`（被 `IoC` 的 `Registry`/`Provider`/`Initialize` 引用）；同时通过包传递的 build props 注入一组 Prism global using（`Prism`、`Prism.Ioc` 等，见 `Core/Common/obj/Debug/Common.GlobalUsings.g.cs` 第 2-11 行），这就是 `IoC.cs` 不写任何 `using` 也能解析 Prism 类型的原因 |
| `Prism.DryIoc.Avalonia` | 9.0.537.11130 | Prism 的 DryIoc 容器实现；本模块源码不直接引用其类型，但它决定了 `IContainerProvider`/`IContainerRegistry` 背后的实际容器是 DryIoc |
| `Serilog` | 4.4.0 | 提供 `ILogger`、`LoggerConfiguration`、`LogEventLevel`，被 `Logger`（Core/Common/Logger.cs）使用 |
| `Serilog.Sinks.Console` | 6.1.1 | 提供 `WriteTo.Console(...)` 控制台 sink（Logger.cs 第 37、44 行） |

### 项目引用

| 依赖 | 路径 | 用途 |
|---|---|---|
| `Abstractions` | `..\Abstractions\Abstractions.csproj`（Common.csproj 第 17 行） | **当前两个源码文件均未引用 Abstractions 的任何类型**（Logger.cs 只用 Serilog，IoC.cs 只用 Prism）。该 ProjectReference 目前对编译不是必需的，属于预留/传递性引用。注意：删除它可能破坏下游项目的传递性依赖解析，待进一步调查下游是否依赖 Common → Abstractions 的传递引用 |

### 编译设置

`<TargetFramework>net10.0</TargetFramework>`、`<ImplicitUsings>enable</ImplicitUsings>`、`<Nullable>enable</Nullable>`（Common.csproj 第 4-7 行）。程序集名/根命名空间由 `Build/Base.props` 的 `_InCore` 分支统一为 `DigitalWorkstation.Core.Common`；输出路径由 `Build/Base.props` 统一管理（Debug 落到 `$(SolutionDir)Output\Debug\`）。

## 被依赖关系（外部 → 本模块）

本模块是面向整个解决方案的基础设施层，按设计意图被以下角色依赖（依据类注释与解决方案结构推断；按本模块深读边界未逐一核实调用点，具体消费项目需在对应模块的深读中确认）：

- **Launcher（shell 宿主，`Launcher/Launcher.csproj`）**：预期在 Prism.Avalonia 应用引导阶段调用 `IoC.Initialize(registry, provider)`——这是 `Initialize` 唯一合法的调用场景（一次性、启动期）。依据：`IoC` 类注释「提供对 Prism 容器注册和解析的全局访问」（Core/Common/IoC.cs 第 4-6 行）。
- **全部 Core 与 Modules 项目**（`Core/Framework`、`Core/Models`、`Core/Resource`、`Core/UIPackage`、`Modules/DashBoard`、`Modules/Workstation` 等）：预期通过 `Logger.*` 静态方法打日志、通过 `IoC.Provider.Resolve<T>()` 解析服务。`Logger`/`IoC` 的纯静态 API 形态决定了消费方零注入成本，这是该层存在的意义。

> 待进一步调查：各项目 `.csproj` 中对 `Common.csproj` 的实际 ProjectReference 清单，以及 `IoC.Initialize` 的真实调用点（属 Launcher/其他模块深读范围）。

## 核心内部数据结构

本模块无内部（非 public）类型。全部类型清单：

| 类型 | 种类 | 文件 |
|---|---|---|
| `Logger` | public class（私有构造 + `Lazy<Logger>` 单例 + 静态方法门面） | Core/Common/Logger.cs |
| `IoC` | public class（私有构造 + `Lazy<IoC>` 单例 + 静态属性/方法门面） | Core/Common/IoC.cs |

### Logger 的字段结构

```
Logger (static 门面)
  └─ SingleInstance: Lazy<Logger>          [static readonly, Logger.cs:16]
       └─ _logger: Serilog.ILogger          [readonly 实例字段, Logger.cs:21]
            构造路径：LoggerConfiguration()
              .MinimumLevel.Information()|Verbose()   ← #if DEBUG 决定
              .MinimumLevel.Override("Microsoft", Warning)
              .Enrich.FromLogContext()
              .WriteTo.Console(outputTemplate)
              .CreateLogger()
```

DEBUG 构建下私有构造函数会创建**两个** Serilog logger（先 Information 配置后 Verbose 配置覆盖 `_logger`，Logger.cs 第 34-46 行），第一个随即成为垃圾——Serilog logger 未实现 `IDisposable` 时的无害浪费，见 pitfalls.md。

### IoC 的字段结构

```
IoC (static 门面)
  └─ InnerInstance: Lazy<IoC>              [static readonly, IoC.cs:14]
       ├─ _registry: IContainerRegistry     [实例字段, null! 初始化, IoC.cs:32]
       ├─ _provider: IContainerProvider     [实例字段, null! 初始化, IoC.cs:33]
       └─ IsInitialized: bool               [实例属性, IoC.cs:56]
  公开面：Registry/Provider 静态属性（get public / set private）转发到上述字段；
          Initialize(registry, provider) 一次性写入并翻转 IsInitialized。
```

### 与 Core/Abstractions 的关系

Common 在工程上引用 Abstractions（见上「项目引用」），但类型层面当前**零耦合**：Abstractions 的全部类型（`IWindowManager`、五个 `I*Contribution` 接口、`ShellRegions` 等，见 docs/analysis/Core/Abstractions/reference.md）都不出现在 Logger.cs/IoC.cs 中。两模块的分工：Abstractions 是纯契约（接口/常量/枚举，无第三方依赖除 Avalonia），Common 是基础设施实现门面（Serilog 日志 + Prism 容器访问）。
