# 启动台进度窗与逐模块异步加载

启动流程保留 DashBoard 启动台作为首个可见窗口，职责改为显示核心模块装载进度：Avalonia 启动后按 `初始化核心服务 → 逐模块加载（显示模块名 + i/N）→ 就绪` 分阶段推进，完成后自动关闭启动台并显示 shell；某模块失败时显示错误并提供"继续（跳过）/退出"。为此放弃 Prism 内置的同步 `InitializeModules` 一次性调用，改为自编排的逐模块异步加载并发布进度事件。

## Considered Options

- **Prism 同步 `InitializeModules` + 手动按钮收尾** —— 拒绝：同步一次性调用无法给出逐模块进度；加载完成后还要求用户点按钮，进度窗失去意义。
- **启动台只加载核心、业务模块按需懒加载** —— 拒绝：进度窗的价值正在于装载过程可见；按需加载会把失败推迟到用户点击时。

## Consequences

- Launcher 引导阶段（AssemblyLoader 预加载）发生在 Avalonia 启动前，无法画进进度窗；进度只覆盖 Avalonia 启动后的阶段。
- 模块的 `OnInitialized` 需容忍异步编排；`ShowMainWindowEvent` 的手动触发流程被自动收尾取代。
