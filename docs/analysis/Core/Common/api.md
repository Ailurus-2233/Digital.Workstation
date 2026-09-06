# Common — 对外接口与调用方式

模块只暴露两个公开类，均在命名空间 `DigitalWorkstation.Core.Common` 下。两个类都是「私有构造 + Lazy 单例 + 静态成员」模式，外部永远拿不到实例，只能调静态成员。

## Logger（Core/Common/Logger.cs）

### 签名清单

| 成员 | 签名 | 说明 |
|---|---|---|
| `Verbose` | `public static void Verbose(string message, string sender = "")` | 发布 Verbose 级别日志（Logger.cs 第 68-71 行） |
| `Debug` | `public static void Debug(string message, string sender = "")` | 发布 Debug 级别日志（第 82-85 行） |
| `Information` | `public static void Information(string message, string sender = "")` | 发布 Information 级别日志（第 96-99 行） |
| `Warning` | `public static void Warning(string message, string sender = "")` | 发布 Warning 级别日志（第 110-113 行） |
| `Error` | `public static void Error(string message, string sender = "")` | 发布 Error 级别日志（第 124-127 行） |
| `Error`（重载） | `public static void Error(Exception? exception, string message, string sender = "")` | 带异常对象的 Error 日志，异常由 Serilog 渲染到 `{Exception}` 占位（第 141-144 行） |
| `Fatal` | `public static void Fatal(string message, string sender = "")` | 发布 Fatal 级别日志（第 155-158 行） |
| `Fatal`（重载） | `public static void Fatal(Exception? exception, string message, string sender = "")` | 带异常对象的 Fatal 日志（第 172-175 行） |

参数语义：
- `message`：日志内容纯文本。内部作为 Serilog 结构化属性 `{Message}` 传入固定模板 `"[{Sender}] {Message}"`，文本中的花括号不会被当模板解析。
- `sender`：日志发送者标记，可选，默认 `""`。内部作为 `{Sender}` 属性输出为 `[sender]` 前缀；不传时输出 `[]`。

### 私有/内部成员（理解行为时需要知道）

- `private static readonly Lazy<Logger> SingleInstance`（第 16 行）：单例载体。
- `private readonly ILogger _logger`（第 21 行）：Serilog logger 实例，私有构造函数中创建。
- `private static Logger Instance => SingleInstance.Value`（第 26 行）。
- `private static ILogger Log => Instance._logger`（第 57 行）：所有公开方法的实际出口。

### 级别可见性

Release 构建最小级别 `Information`（`Verbose`/`Debug` 调用被 Serilog 直接丢弃）；DEBUG 构建最小级别 `Verbose`（全级别可见）。这是编译期 `#if DEBUG` 决定（Logger.cs 第 39-46 行），不是运行时配置。

### 输出通道（sink）

`Logger` 唯一的输出通道是 Console sink——私有构造函数中只调用了 `.WriteTo.Console(outputTemplate: ...)`（Logger.cs 第 37、44 行各一处），没有文件、网络或调试窗口等任何其他 sink。要加落盘日志必须新增 `Serilog.Sinks.File` 包并修改构造函数（DEBUG/Release 两条配置链都要改）。

### 调用示例

```csharp
using DigitalWorkstation.Core.Common;

Logger.Information("模块加载完成", "DashBoardModule");
Logger.Error(ex, "配置文件读取失败", "SettingsLoader");
```

无初始化/生命周期要求：`Lazy` 保证首次调用任意方法时才构建 Serilog logger；无 `IDisposable`/`Flush`/`CloseAndFlush` 暴露。

## IoC（Core/Common/IoC.cs）

### 签名清单

| 成员 | 签名 | 说明 |
|---|---|---|
| `Registry` | `public static IContainerRegistry Registry { get; private set; }` | Prism 容器注册接口（用于 `RegisterSingleton`/`Register` 等）。getter 转发到 `Instance._registry`（IoC.cs 第 38-42 行） |
| `Provider` | `public static IContainerProvider Provider { get; private set; }` | Prism 容器解析接口（用于 `Resolve<T>()` 等）。getter 转发到 `Instance._provider`（第 47-51 行） |
| `Initialize` | `public static void Initialize(IContainerRegistry registry, IContainerProvider provider)` | 一次性注入两个容器接口；已初始化时抛 `InvalidOperationException("IoC is already initialized")`（第 64-74 行） |

`IContainerRegistry`/`IContainerProvider` 来自 Prism（命名空间 `Prism.Ioc`，由 Prism.Avalonia 包传递注入的 global using 提供，见 `obj/Debug/Common.GlobalUsings.g.cs` 第 7 行）。底层容器是 DryIoc（`Prism.DryIoc.Avalonia` 包引用）。

### 私有/内部成员

- `private static readonly Lazy<IoC> InnerInstance`（第 14 行）+ `private static IoC Instance`（第 19 行）：单例。
- `private IContainerRegistry _registry = null!;`（第 32 行）、`private IContainerProvider _provider = null!;`（第 33 行）：`null!` 声明的非空字段，初始化前实际为 `null`。
- `private bool IsInitialized { get; set; }`（第 56 行）：一次性闸门，注意它是**实例属性**（依赖单例唯一性才等价于全局状态）。

### 生命周期要求（强约束）

1. `Initialize` 必须在应用启动引导阶段（Prism 应用创建容器后）调用**恰好一次**。
2. 任何对 `IoC.Registry`/`IoC.Provider` 的访问必须发生在 `Initialize` 之后，否则拿到 `null` 引用，首次成员调用即 `NullReferenceException`（字段以 `null!` 抑制了编译器可空警告，运行时不保护）。
3. 无注销/复位 API——初始化后两个引用存活到进程结束。

### 调用示例

引导侧（预期在 shell/Launcher 的 Prism 应用中，调用点不在本模块内）：

```csharp
// PrismApplication 容器就绪后，一次性调用
IoC.Initialize(containerRegistry, containerProvider);
```

消费侧：

```csharp
var windowManager = IoC.Provider.Resolve<IWindowManager>();
IoC.Registry.RegisterSingleton<IMyService, MyService>();
```

## 对外数据结构

本模块不定义任何 DTO/结构体/枚举。对外数据只有：

- `Logger`/`IoC` 两个类的静态成员签名（见上表）；
- 透传的 Prism 类型 `IContainerRegistry`/`IContainerProvider`（Prism 程序集定义，本模块不包装）。

Console sink 输出格式（对外可观察的"数据格式"）：`[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}`（Logger.cs 第 37、44 行），例如 `[12:03:45 INF] [DashBoardModule] 模块加载完成`。
