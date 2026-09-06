# Common — 不变量与陷阱

## 隐含不变量

1. **`IoC.Initialize` 恰好一次、先于一切容器访问**（Core/Common/IoC.cs 第 64-74 行）。两次调用抛 `InvalidOperationException`；零次调用则 `Registry`/`Provider` 拿到 `null`（字段 `= null!;` 第 32-33 行，可空分析被抑制，编译期零警告）。模块自身没有任何「先检查是否初始化」的保护性 API。
2. **`IsInitialized` 是实例属性**（IoC.cs 第 56 行），不是静态字段。它能作为全局闸门完全依赖 `Lazy<IoC>`（第 14 行）保证单例唯一——如果哪天有人把单例模式改掉（比如允许 `new IoC()`），闸门立刻失效而编译器不会报错。
3. **`Registry`/`Provider` 的 setter 是 `private`**（IoC.cs 第 41、50 行）。类外想替换容器引用（如测试场景）没有合法路径，只能走 `Initialize`——而 `Initialize` 又只允许一次。即：**当前设计下 IoC 状态不可复位**，进程级测试隔离是唯一选择。
4. **`IoC.Initialize` 的检查-设置不是原子的**（第 66-73 行：先查 `IsInitialized` 再赋值再翻转标志）。`Lazy` 只保证实例唯一，不保证 `Initialize` 的并发安全——两个线程同时首次调用可能都通过检查。引导期单线程调用是隐含前提。
5. **Logger 无释放语义**：Serilog logger 构建后不 `CloseAndFlush`、不暴露 `IDisposable`。Console sink 是同步直写，进程退出一般不丢日志；但若将来加异步 sink（如 `Serilog.Sinks.Async` 或 File），退出丢日志会成为新坑，届时必须引入生命周期管理。
6. **DEBUG 与 Release 是两套硬编码配置链**（Logger.cs 第 34-45 行）：改 sink、改模板、改 Override 时**两处必须同步改**，只改一处会造成「调试时正常、发布后日志消失/格式不同」。

## 易错改法

1. **在 `#if DEBUG` 外改配置**：Logger.cs 构造函数先无条件构建一个 Information 级 logger（第 34-38 行），DEBUG 下再整体重建覆盖（第 41-45 行）。若只在第一处加 `.WriteTo.File(...)`，Debug 构建下文件 sink 根本不生效——因为 `_logger` 被第二个配置覆盖了。这是当前代码结构埋的坑（第一个 logger 对象在 DEBUG 下纯属浪费分配）。
2. **以为 `sender` 参数会拼进消息文本**：所有方法用 Serilog 结构化模板 `"[{Sender}] {Message}"`（如 Logger.cs 第 98 行），`sender`/`message` 是属性不是字符串插值；但输出模板 `{Message:lj}`（第 37 行）按字面渲染，所以最终文本形如 `[Sender] message`，与直觉一致——只是改模板时要理解 `:lj` 的含义（literal，不带引号），去掉 `l` 会让字符串属性带引号输出。
3. **给 IoC 加 `using Prism.Ioc;` 之外的「修复」**：IoC.cs 全文无 using 是**故意的**——Prism 命名空间由 `Prism.Avalonia` 包传递的 build props 以 global using 注入（见 `Core/Common/obj/Debug/Common.GlobalUsings.g.cs`）。若把 `Prism.Avalonia` 包引用从 Common.csproj 移除（比如觉得「只用了接口应该用更小的 Prism.Core」），`IContainerRegistry` 会立刻无法解析。
4. **删除 Common → Abstractions 的 ProjectReference**：当前源码确实没用到 Abstractions 类型（见 reference.md），删除后本模块仍能编译，但下游项目若依赖经 Common 传递的 Abstractions 引用会断。改前先搜下游 `.csproj`。
5. **把 `Logger`/`IoC` 改成 `static class`**：看似合理（全是静态成员），但当前实现是「私有构造 + 实例字段 + 静态转发」——改成 static class 需要把 `_logger`/`_registry`/`_provider`/`IsInitialized` 全部改静态，并失去 `Lazy<T>` 的初始化语义（需改用静态构造函数或显式初始化检查）。行为等价性需要逐字段核对，不是机械改写。

## 历史踩坑（代码中透露的）

1. **DEBUG 下双份 logger 构建**（Logger.cs 第 34-46 行）：第一个 `LoggerConfiguration()...CreateLogger()` 在 DEBUG 构建中创建后立即被第二个覆盖，是无害但浪费的分配。注释「调试模式下使用更详细的日志级别」（第 40 行）表明作者意图是级别切换，但实现选择了整体重建而非参数化 `MinimumLevel`——说明这段代码经历过「Release 也要能编译运行」与「Debug 要 Verbose」的折中，重构时应合并为单一配置链 + 条件级别。
2. **`null!` 抑制符**（IoC.cs 第 32-33 行）：作者明确知道字段初始化前为 null，用 `!` 压住可空警告而不是引入「未初始化」显式状态。这是防御性提示：读这两个属性的代码必须自己保证时序，编译器帮不了你。
3. **`Output/Debug/` 空目录遗留**（Core/Common/Output/）：构建输出曾落在模块目录内，后由 `Build/Base.props` 统一到仓库根 `Output/`。目录残留说明迁移发生过——不要在模块内新建输出目录假设。
