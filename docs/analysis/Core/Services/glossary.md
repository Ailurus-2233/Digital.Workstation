# Services — 术语与代码定位

- 基础服务：应用与模块共同依赖、由框架先行准备的通用能力。统一入口 BasicServices。
- 服务登记：将类型与构造规则加入容器，不等同于加载配置或启动服务。
- 服务启动：在明确的生命周期时机准备运行状态；BasicServices.Start 加载设置与插件启动选择。
- 配置 owner：ConfigurationPersistence，管理多个单文件写入器的统一保存和释放。
- 贡献批次：ShellContributionCatalog/ContributionBatch 对模块贡献的登记、准备、提交或拒绝状态；不回滚普通容器登记。
- 插件启动快照：PluginStartupService 本次启动加载选择与发现结果；页面修改只影响下次启动。
- IModuleService：模块附加服务的同步生命周期契约，Start 准备运行状态，Stop 清理已启动或部分启动状态。
- ModuleServiceCatalog：按批次保存服务登记、每批单次启动与全局停止责任的目录。
- 启动责任：服务实例一经构造成功，在调用 Start 前即纳入停止列表，保证部分启动失败可被清理。
- 逆序停止：失败时逆序处理当前模块，退出时逆序处理全局仍在运行的服务，然后停止基础服务。
