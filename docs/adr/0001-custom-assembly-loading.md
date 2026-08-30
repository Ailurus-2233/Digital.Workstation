# 自定义程序集加载与分类输出布局

Release 构建不采用 .NET 默认的平铺输出与 probing：项目引用不复制到本地（`Private=false`），NuGet 依赖按包名分类归档到 `libraries/<分类>/`，Core/Modules 项目分别输出到 `core/`、`modules/`，原生资产归入 `runtimes/<rid>/native/`；进程启动时由 Launcher 内的 AssemblyLoader 预加载引导程序集、初始化搜索路径，并注册 `AppDomain.AssemblyResolve` 与 `DllImportResolver` 按需补位。

选择此方案的原因是让部署产物按角色有序组织（应用、核心框架、第三方依赖、原生资产各归其位），代价是必须维护一套自定义加载逻辑，且任何加载问题都无法依赖运行时默认行为兜底。默认平铺 probing 是被明确拒绝的替代方案。

## Consequences

- 新增项目/依赖类别时，需同时检查 `Build/` 下的分类 target 与 `AssemblyLoader` 的搜索路径是否覆盖。
- 引导程序集清单（`BootRequiredAssemblyFiles`）需手工维护；新增"最早阶段就要用"的程序集时必须同步更新。
