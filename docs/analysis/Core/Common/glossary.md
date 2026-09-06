# Common — 术语表

## 模块特有术语与缩写

| 术语 | 定义 | 首次出现位置 |
|---|---|---|
| `IoC` | Inversion of Control，此处专指 Prism 依赖注入容器的**全局静态访问器类** `DigitalWorkstation.Core.Common.IoC`，不是泛指控制反转原则 | Core/Common/IoC.cs 第 7 行 |
| `Registry` | `IoC.Registry`，Prism `IContainerRegistry` 的静态访问点，语义是「容器的**注册**侧」（`RegisterSingleton`/`Register` 等写操作） | Core/Common/IoC.cs 第 38 行 |
| `Provider` | `IoC.Provider`，Prism `IContainerProvider` 的静态访问点，语义是「容器的**解析**侧」（`Resolve<T>()` 等读操作） | Core/Common/IoC.cs 第 47 行 |
| `Initialize` | `IoC.Initialize(registry, provider)`，容器引用的**一次性注入点**；本项目语境下「初始化 IoC」= 把 Prism 容器两个接口交给静态门面，不是创建容器本身（容器由 Prism 引导创建） | Core/Common/IoC.cs 第 64 行 |
| `sender` | `Logger.*` 方法的第二参数，日志**来源标记**字符串（模块名/类名等），渲染为消息前缀 `[sender]`；默认 `""` 时输出 `[]` | Core/Common/Logger.cs 第 68 行 |
| `Log` | `Logger` 内的**私有**静态出口属性（`Instance._logger`），不是公开 API；公开面是 `Verbose`/`Debug`/`Information`/`Warning`/`Error`/`Fatal` 方法 | Core/Common/Logger.cs 第 57 行 |
| Sink | Serilog 术语，日志**输出目的地**。本模块唯一 sink 是 Console（`WriteTo.Console`，Logger.cs 第 37、44 行），由 `Serilog.Sinks.Console` 包提供 | Core/Common/Logger.cs 第 37 行 |
| `LogEventLevel` | Serilog 的级别枚举（Verbose < Debug < Information < Warning < Error < Fatal）。本模块用它做 `Microsoft` 命名空间的 Override 过滤（压到 `Warning`） | Core/Common/Logger.cs 第 35 行 |
| `Lazy<T>` 单例 | 两类型共用的单例实现手法：`private static readonly Lazy<T>` + 私有构造函数 + 静态成员转发实例字段。延迟到首次访问才初始化 | Logger.cs 第 16 行、IoC.cs 第 14 行 |
| `null!` | C# 可空抑制符。`_registry`/`_provider` 用它声明「编译时当非空、运行时先为 null」，把初始化时序责任转移给调用方 | Core/Common/IoC.cs 第 32-33 行 |

## 与同名通用概念的区别

- **`IoC` ≠ 依赖注入原则**：在别的项目里"IoC 容器"通常通过构造函数注入使用；本项目中 `IoC` 是一个具体类，代表 Service Locator 式的全局静态访问。消费代码写 `IoC.Provider.Resolve<T>()` 而不是注入 `IContainerProvider`。
- **`Logger` ≠ `Serilog.ILogger`**：本项目的 `Logger` 是自有静态门面类（DigitalWorkstation.Core.Common.Logger），不实现/不暴露 Serilog 接口；Serilog 的 `ILogger` 被完全包在私有字段 `_logger` 里。
- **`Initialize` ≠ 容器创建**：Prism 应用引导时容器由 `Prism.DryIoc.Avalonia` 创建；`IoC.Initialize` 只是把已存在容器的两个接口**登记**到静态门面。
- **`Debug` 一词三义**：① `Logger.Debug(...)` 是日志级别方法；② `#if DEBUG` 是编译符号（Logger.cs 第 39 行）决定最低级别为 Verbose；③ `obj/Debug/` 是构建目录。语境区分。
- **`Common` ≠ 通用工具库**：按当前内容它只含日志与 IoC 两个基础设施门面，不是放扩展方法/帮助类的杂项库（Core/Common/ 目录下仅此两个 .cs 文件）。

## 代码命名 ↔ 业务概念对应

| 代码元素 | 业务/架构概念 |
|---|---|
| `DigitalWorkstation.Core.Common` 命名空间 | 「Core 层共享基础设施」——由 `Build/Base.props` 的 `_InCore` 规则按目录路径自动生成 |
| `Logger.Verbose/Debug/Information/Warning/Error/Fatal` | 与 Serilog `LogEventLevel` 六级一一对应，语义直接继承 Serilog |
| `IoC.Registry` / `IoC.Provider` | Prism 容器的写/读两侧：Registry= composition time（注册期），Provider= resolution time（运行期解析） |
| `IsInitialized` | 引导流程状态机的一比特：false=容器未登记（访问即 NRE），true=已登记（再登记即抛异常） |
| `"Microsoft"` Override | 过滤第三方（.NET/ASP.NET 风格命名空间）噪音日志的约定，压到 Warning 级 |
