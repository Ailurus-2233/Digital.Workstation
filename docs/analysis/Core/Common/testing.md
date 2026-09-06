# Common — 验证方式

## 本模块的测试在哪

**没有。** 解决方案（`Digital.Workstation.slnx`）中唯一的测试项目是 `UnitTest/Framework/Framework.csproj`（`/_UnitTest/` 文件夹），它面向 Core/Framework 模块，不覆盖 Core/Common。`Core/Common/` 目录下没有任何测试文件。

如实说明：Logger 是 Serilog 的薄静态封装、IoC 是 Prism 容器引用的薄静态持有者，目前靠编译与运行时冒烟保证正确性。

## 怎么跑（若需跑全仓现有测试）

```powershell
dotnet test UnitTest/Framework/Framework.csproj
```

与本模块无关，仅说明仓库现状；没有可筛选的 Common 测试。

## 改完代码后的最小验证集

本仓库为纯桌面端（Avalonia），约定**不做 UI 自动化测试验收**；验证聚焦数据/行为检测点：

1. **编译**：`dotnet build Core/Common/Common.csproj` 通过（Debug 与 Release 各一次——`Logger` 有 `#if DEBUG` 分支，两种配置走的是不同的 Serilog 配置链，Logger.cs 第 34-46 行）。
2. **日志冒烟**（改了 Logger.cs 必做）：启动应用（Launcher），确认控制台出现 `[HH:mm:ss LVL] [sender] message` 格式的输出；改动级别配置时确认 `Logger.Debug`/`Verbose` 在 Debug 可见、Release 被丢弃。
3. **IoC 冒烟**（改了 IoC.cs 必做）：启动应用确认引导处 `IoC.Initialize` 调用一次成功、消费方 `IoC.Provider.Resolve<T>()` 正常返回；人为二次调用 `Initialize` 应抛 `InvalidOperationException("IoC is already initialized")`（IoC.cs 第 68 行）——这是该类的核心数据不变量，值得用一个 throwaway 控制台小程序验证。

## 测试约定（若要新增测试）

仓库现有约定：测试项目放 `UnitTest/<模块名>/`，程序集名/命名空间由 `Build/Base.props` 的 `_InUnitTest` 分支自动生成为 `DigitalWorkstation.UnitTest.<模块名>`，输出到 `UnitTest/Output/$(Configuration)/`。

针对本模块的可测数据点（不碰 UI）：
- `IoC.Initialize` 一次性守卫：第二次调用抛 `InvalidOperationException` 且消息为 `"IoC is already initialized"`；
- 未初始化时 `IoC.Registry`/`IoC.Provider` 返回 `null`（`_registry`/`_provider` 为 `null!`，IoC.cs 第 32-33 行）——当前是隐式行为，测试会把它固化；
- `Logger` 属静态全局状态，单测难以隔离（Serilog logger 在私有构造函数内硬编码构建，无注入点），如需可测性应先重构（引入内部可替换的 `ILogger` 工厂），这本身是个改造决策，见 pitfalls.md。
