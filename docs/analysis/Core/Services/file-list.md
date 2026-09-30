# Services — 文件清单

相对 Core/Services；构建产物不入清单。

- ApplicationRestarter.cs：配置保存后启动新进程并正常退出
- BasicServices.cs：基础服务登记、显式启动和退出保存
- Contributions/ShellContributionCatalog.cs：贡献目录、登记扩展与内部批次
- Layout/LayoutPersistence.cs：布局容错读取及写入/删除入口
- Layout/ShellLayoutDto.cs：布局格式版本 1 的存储 DTO
- Persistence/ConfigurationPersistence.cs：配置写入器统一 owner
- Persistence/DebouncedJsonFile.cs：防抖保存、强制刷新、删除与释放
- Plugins/PluginDescriptor.cs：内部发现结果及错误描述
- Plugins/PluginDiscovery.cs：元数据筛选、禁用过滤与入口加载
- Plugins/PluginLoadContext.cs：插件私有依赖隔离与共享程序集身份
- Plugins/PluginStartupInfo.cs：插件管理页读取的快照 record
- Plugins/PluginStartupService.cs：下次启动选择、持久化与本次快照
- Services.csproj：项目依赖、内部可见性与插件共享清单嵌入
- Settings/SettingCatalog.cs：当前可见设置声明、重复 Id 与分组规则
- Settings/SettingsService.cs：设置内存值、启动快照、重启标记与事件
- WindowManager/FrameworkWindowManager.cs：窗口映射与主窗口双接口实现

- Lifecycle/ModuleServiceRegistration.cs：模块附加服务单例登记扩展，共用贡献批次闸门。
- Lifecycle/ModuleServiceCatalog.cs：批次内启动、失败清理、全局逆序停止与英文生命周期日志。
