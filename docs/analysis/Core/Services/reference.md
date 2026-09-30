# Services — 依赖关系

Services.csproj 直接引用 Abstractions.csproj 与 Common.csproj，Prism/Avalonia 经 Common 传递提供。无 Framework 引用。

Framework → Services → Common → Abstractions。Modules 和 Plugins 通过 Framework 的传递引用获取 Services；模块登记贡献使用 Services.Contributions，UI 扫描扩展仍在 Framework。

Services.csproj 将 Build/PluginSharedAssemblies.txt 嵌入为 DigitalWorkstation.Core.Services.Plugins.SharedAssemblies。PluginLoadContext 从自身程序集读取；构建过滤与运行时共享范围同源。DigitalWorkstation.Core.* 规则已覆盖 Services，无需新增共享模式。

项目位于 Core 下，沿用 Build/Base.props：Debug 平铺输出，Release 输出 core/DigitalWorkstation.Core.Services.dll。Launcher 的已有 core 搜索路径覆盖它，不修改宿主解析器或插件上下文前缀。

Services 向 DigitalWorkstation.Core.Framework 授予内部可见性，供启动编排调用贡献批次和插件发现。普通消费模块使用公开契约及登记扩展。
## 生命周期依赖

IModuleService 位于 Abstractions.Services，只有 Start/Stop，不引入 Prism 包依赖。ModuleServiceRegistration 和 ModuleServiceCatalog 位于 Services.Lifecycle，经 Common 获得 Prism 容器接口。

ModuleServiceCatalog 复用 Services.Contributions 的 ContributionBatch 作为归属，不维护第二个 AsyncLocal。BasicServices.Register 登记目录；Framework 负责模块 Start、失败 Stop 和真实 Exit 的 Dispose。Dispose 模块目录先于 BasicServices.Dispose。

SampleModuleService 属于 Sample 插件程序集，经宿主共享的 Abstractions 接口参与生命周期；服务实例由同一宿主容器解析，SampleStatusBarItem 注入它并验证启动状态。
