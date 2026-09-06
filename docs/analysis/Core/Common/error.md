# Common — 异常与排查

## 模块可能抛出的异常

### 1. `InvalidOperationException` — IoC 重复初始化

- **抛出位置**：`IoC.Initialize`（Core/Common/IoC.cs 第 66-69 行），消息固定为 `"IoC is already initialized"`。
- **触发条件**：`Instance.IsInitialized` 已为 `true` 时再次调用 `Initialize`。
- **常见原因**：① 引导代码被意外执行两次（如测试宿主与真实宿主并存）；② 模块加载回调里误调 `Initialize`（它只允许 Prism 容器创建后调一次）；③ 单元测试之间共享了进程且都尝试初始化。
- **排查**：全局搜索 `IoC.Initialize` 的调用点，正常应只有一个（启动引导处）；异常栈顶即 `IoC.Initialize`，顺栈往下找第二个调用者。

### 2. `NullReferenceException` — 初始化前访问容器

- **抛出位置**：不在本模块内——`IoC.Registry`/`IoC.Provider` 的 getter（IoC.cs 第 40、49 行）在 `Initialize` 之前返回 `null`（字段 `_registry`/`_provider` 声明为 `= null!;`，第 32-33 行，编译器可空检查被 `!` 抑制），异常在**消费方**首次调用容器成员时抛出（如 `IoC.Provider.Resolve<T>()`）。
- **触发条件**：`Initialize` 之前（或 Initialize 因异常未完成）就有代码访问容器。
- **常见原因**：① 静态构造函数/字段初始化器等早于引导的代码路径解析服务；② 引导顺序变动后 `Initialize` 调用点被推迟。
- **排查**：异常栈会指向消费方而非 IoC.cs；确认 `IoC.Initialize` 在应用启动路径上先于任何 `IoC.Provider`/`IoC.Registry` 访问执行。可用 `Logger.Information` 在 `Initialize` 前后打标记日志确认时序。

### 3. Logger 本身不抛业务异常

- `Logger` 的全部公开方法（`Verbose`…`Fatal`，Core/Common/Logger.cs 第 68-175 行）不设防也不捕获：参数 `message` 传 `null` 时行为由 Serilog 决定（渲染为空），不会由本模块抛出异常；`Error(Exception? ...)`/`Fatal(Exception? ...)` 的 `exception` 形参本身就标注可空。
- 理论上的初始化期异常：若 Serilog 配置无效（本模块配置是硬编码的，正常不会），异常会在**首次**调用任何 `Logger.*` 方法时从私有构造函数经 `Lazy<Logger>`（第 16 行）抛出，并被 `Lazy` 缓存——之后每次访问都重抛同一异常。表现是「第一次打日志就炸，且永远炸」。
- **排查**：看首次日志调用点；检查是否有人改动了 Logger.cs 第 34-45 行的 `LoggerConfiguration` 链（如加了不存在的 sink 包而忘加 PackageReference）。

## 错误处理路径

- 本模块**没有 try/catch**。两条类的策略一致：故障快速暴露（fail-fast）——IoC 用异常拒绝非法状态（重复初始化），Logger 把渲染交给 Serilog 不做防御。
- 日志不是错误处理通道的替代品：`Logger.Error(ex, ...)`（Logger.cs 第 141-144 行）只是记录，异常是否继续传播完全由调用方决定。
- 上游（消费方）典型模式：`catch` 住业务异常 → `Logger.Error(ex, "...", "来源")` → 视情况重抛或降级。本模块不参与该决策。

## 排查入口速查

| 症状 | 看哪里 |
|---|---|
| 控制台没有任何日志 | ① 进程是否有附加控制台（GUI 子系统下 Console sink 无处可写，见 pitfalls.md）；② 是否 Release 构建却用 `Logger.Debug`/`Verbose`（Logger.cs 第 34 行 `MinimumLevel.Information()` 会丢弃）；③ `Microsoft.*` 日志被 Override 压到 Warning（第 35 行） |
| `IoC is already initialized` | 搜全部 `IoC.Initialize` 调用点 |
| 访问容器 NRE | 确认 `Initialize` 先于访问执行；注意静态初始化器时序 |
| 日志格式/级别异常 | Logger.cs 私有构造函数（第 31-48 行），注意 DEBUG/Release 两条配置链都要看 |
