# Common — 模块简述

## 模块做什么

Core/Common 是 Digital.Workstation 的基础设施静态门面层，整个模块只有两个公开类，位于 `Core/Common/Logger.cs` 和 `Core/Common/IoC.cs`：

1. **`Logger`**（Core/Common/Logger.cs）——基于 Serilog 的静态日志门面。进程内单例，把日志写到控制台 sink，向全解决方案暴露 `Verbose`/`Debug`/`Information`/`Warning`/`Error`/`Fatal` 六个级别的静态方法（`Error`/`Fatal` 各有一个带 `Exception?` 的重载），调用方不需要持有任何日志实例。
2. **`IoC`**（Core/Common/IoC.cs）——Prism 依赖注入容器的静态访问器。进程内单例，持有 Prism 的 `IContainerRegistry`（注册侧）与 `IContainerProvider`（解析侧）两个引用，通过一次性 `IoC.Initialize(registry, provider)` 注入，之后任何代码可通过 `IoC.Registry` / `IoC.Provider` 全局访问容器。

命名空间为 `DigitalWorkstation.Core.Common`（由 `Build/Base.props` 中 `_InCore` 分支按目录规则生成：`DigitalWorkstation.Core.$(MSBuildProjectName)`）。目标框架 `net10.0`（Core/Common/Common.csproj）。

## 核心设计逻辑

- **静态门面 + Lazy 单例**：两个类都用同一个模式——`private static readonly Lazy<T>` 私有实例（Logger.cs 第 16 行 `SingleInstance`、IoC.cs 第 14 行 `InnerInstance`）+ 私有构造函数 + 一组 `static` 公开成员转发到实例字段。设计理由：日志与 DI 容器是进程级基础设施，任何模块都需要，用静态访问器避免把 `ILogger`/`IContainerProvider` 沿构造函数层层传递；用 `Lazy<T>` 而非静态构造函数是为了让初始化延迟到首次访问（Logger 的 Serilog 配置在首次打日志时才构建）。
- **DEBUG/Release 双配置**：`Logger` 私有构造函数（Logger.cs 第 31-48 行）先按 Release 配置构建 Serilog logger（最小级别 `Information`），`#if DEBUG` 分支内再整体重建一个 `Verbose` 级别的 logger 覆盖字段。用编译期条件而非运行时开关，发布版完全不含 Verbose 配置代码。**两条配置链除最小级别（`Information` vs `Verbose`）外完全相同**：同样的 `.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)`、同样的 `.Enrich.FromLogContext()`、同样的 `.WriteTo.Console(...)` 和同一个 `outputTemplate`——只是逐字写了两遍（Logger.cs 第 34-38 行与第 41-45 行一一对应）。
- **setter 收窄**：`Registry`/`Provider` 是 `public static` 读、`private set` 写（IoC.cs 第 38-51 行），外部只能读不能改，写入口唯一即 `Initialize`。
- **结构化日志但固定模板**：所有公开方法把 `sender` 与 `message` 作为 Serilog 结构化属性传入固定模板 `"[{Sender}] {Message}"`（如 Logger.cs 第 98 行 `Log.Information("[{Sender}] {Message}", sender, message)`），`sender` 默认 `""`。设计理由：统一日志格式并保留来源标记，同时屏蔽 Serilog 的模板语法，调用方只传纯文本。
- **权衡**：只有 Console sink（`Serilog.Sinks.Console`），没有文件 sink——桌面应用阶段优先开发期可见性，代价是终端外运行（GUI 子系统无附加控制台）时日志不可见；也没有把 Serilog 的 `ILogger` 暴露给 Prism 容器做构造函数注入，选择了更简单的全局静态访问。

## 状态流转

**日志路径**：任意调用方 → `Logger.Information(message, sender)` 等静态方法 → 私有静态属性 `Log`（Logger.cs 第 57 行）触发 `SingleInstance.Value` → 首次访问时私有构造函数构建 Serilog `ILogger`（`LoggerConfiguration().MinimumLevel…().WriteTo.Console(...).CreateLogger()`）→ `ILogger` 按级别方法写入 → Console sink 按模板 `[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}` 输出到标准输出。模块自身无可变状态；唯一状态是懒建的 `_logger` 字段，构建后不再变化。

**IoC 路径**：应用启动（Prism.Avalonia 引导，调用点在本模块之外）→ `IoC.Initialize(registry, provider)` → 检查 `Instance.IsInitialized`，未初始化则通过私有 setter 写入 `Instance._registry`/`Instance._provider`（IoC.cs 第 32-33 行，声明为 `null!` 的非空引用字段）并置 `IsInitialized = true` → 之后任意消费方 `IoC.Provider.Resolve(...)` 解析服务、`IoC.Registry.Register...(...)` 追加注册。副作用：`IsInitialized` 一旦为 `true` 不可复位（无 Reset API）。

## 常见修改场景

1. **要加文件日志（落盘排查现场问题）**：改 `Core/Common/Common.csproj` 加 `Serilog.Sinks.File` 包引用；改 `Core/Common/Logger.cs` 私有构造函数（第 34-45 行两处 `LoggerConfiguration` 链都要改，否则 DEBUG/Release 行为不一致）追加 `.WriteTo.File(...)`。
2. **要改日志最低级别或过滤第三方命名空间**：改 `Core/Common/Logger.cs` 构造函数中的 `.MinimumLevel.Information()`/`.MinimumLevel.Verbose()`（第 34、41 行）和 `.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)`（第 35、42 行）。
3. **要让单元测试能重置 IoC 容器**：改 `Core/Common/IoC.cs`——当前 `IsInitialized`（第 56 行）无复位路径，需新增内部/公开 `Reset()` 方法清 `_registry`/`_provider`/`IsInitialized`，注意 `Registry`/`Provider` 的 setter 是 `private`（第 41、50 行），只能在类内操作。
4. **要新增日志级别方法（如带属性包的结构化日志）**：在 `Core/Common/Logger.cs` 的 `#region Public API`（第 52 行起）仿照 `Information`（第 96-99 行）加静态方法，内部走 `Log`（第 57 行）。
5. **要改日志输出模板**：改 `Core/Common/Logger.cs` 第 37、44 行的 `outputTemplate` 字符串（两处必须同步改，DEBUG 与 Release 各一份）。
