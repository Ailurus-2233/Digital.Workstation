# Debug 构建跳过自定义程序集解析

`AssemblyLoader.IsDesignEnvironment()` 在 `#if DEBUG` 下恒返回 `true`，因此**任何** Debug 运行（不只是设计器/预览器场景）都跳过整个自定义加载流程（引导程序集预加载、搜索路径、AssemblyResolve、native 预加载），完全依赖 .NET 默认 probing。这是有意设计，不是历史遗留。

原因：Debug 输出平铺且不套用分类布局（见 ADR-0001），默认 probing 天然可用；让调试路径走运行时标准行为，可以避免自定义加载逻辑干扰断点、设计器与热重载等开发期体验。Release 运行才走完整引导流程。

## Consequences

- 自定义加载逻辑的 bug 只在 Release 构建下显现；调试加载问题必须构建 Release。
- 不要"修复"这个短路让它在 Debug 下也启用——那是刻意的开发期行为。
