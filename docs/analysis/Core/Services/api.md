# Services — 对外接口

统一启动入口：BasicServices.Register(IContainerRegistry) 注册基础单例；解析 BasicServices 后调用 Start()；真正退出时调用 Dispose()。这三个操作由 Framework 编排。Start 幂等，Dispose 幂等；已 Dispose 后 Start 抛 ObjectDisposedException。

| 命名空间 | 接口与调用 |
|---|---|
| DigitalWorkstation.Core.Services.Settings | SettingsService 实现 ISettingsService，Get/Set/IsPendingRestart 保持原签名；Load 由 BasicServices.Start 调用。SettingCatalog.Find/GetItems/GetGroups 查询当前可见贡献，重复 Id 首个有效声明生效 |
| DigitalWorkstation.Core.Services.Layout | LayoutPersistence.Load() 返回 ShellLayoutDto 或 null；ScheduleSave(dto) 安排防抖保存；Delete() 作废旧快照并删除。DTO 格式版本仍为 1，PanelAlignment 使用 Abstractions.Layout 契约 |
| DigitalWorkstation.Core.Services.Persistence | ConfigurationPersistence.FlushPending() 返回所有文件保存是否成功；Dispose() 保存并释放计时器；单文件机制为 internal |
| DigitalWorkstation.Core.Services.Contributions | RegisterShellContribution<T>(factory)、RegisterShellContribution<T,TImplementation>()；ShellContributionCatalog.Get<T>() 读取已准备且可见贡献。批次入口仅向 Framework 开放 |
| DigitalWorkstation.Core.Services.WindowManager | FrameworkWindowManager 实现 IWindowManager 与 IMainWindowManager，方法签名不变；双接口同实例，主窗口仍由 Framework 在启动序列登记 |
| DigitalWorkstation.Core.Services.Plugins | PluginStartupService.Plugins 提供发现快照；IsEnabled/SetEnabled/IsPendingRestart 管理下次启动选择；内部 Load 由 BasicServices.Start 调用；发现和本次启动过滤只向 Framework 开放 |
| DigitalWorkstation.Core.Services | ApplicationRestarter.Restart() 先 FlushPending，成功后启动相同进程命令并退出；保存失败留在当前进程 |

Framework 的设置声明 GeneralSettings、UiLanguage、SettingRegistration 和 ShellContributionCollector 未迁移。稳定语言 Id、配置路径及格式保持原值。不要把服务实现的程序集名当作设置身份。
## 模块附加服务

契约：DigitalWorkstation.Core.Abstractions.Services.IModuleService，void Start() / void Stop()。Start 和 Stop 由 Framework 在 UI 线程调用；Start 部分成功后抛异常也调用 Stop，Stop 必须容忍未完全启动的状态。

登记扩展：DigitalWorkstation.Core.Services.Lifecycle.ModuleServiceRegistration.RegisterModuleService<TService>(IContainerRegistry)，约束 TService : class, IModuleService。在模块的 RegisterTypes 中调用；单例可被贡献构造注入，启动成功后才开始贡献准备。真实例子是 Plugins/Sample/SamplePlugin.cs 对 SampleModuleService 的登记。

需要业务接口别名时，先登记具体服务，再以工厂解析同一个具体单例；不要另注册一个接口到实现的独立单例。普通容器登记不会自动参与生命周期。

ModuleServiceCatalog 是基础登记提供的单例目录；模块不自行调用 Dispose。批次 Start/Stop 为 internal，Framework 在入口初始化后启动、准备失败时停止、真实退出时统一 Dispose。每批只启动一次，同服务实现类型只允许一次登记。没有动态卸载或失败批次重试协议。

## 插件名称、Key 与卸载文件

PluginDiscovery 在 PE 元数据中读取 PluginAttribute 的稳定 Key、资源类型与名称资源键，不为读取禁用项名称而加载程序集。PluginDescriptor.Name 保留入口全名，DisplayName 为显示名称，Key 为稳定配置标识；PluginStartupInfo.Id 为 Key、Name 为显示名称。旧入口全名配置通过 MigrateIdentity 迁移到 Key，已有 Key 值优先。候选 Key 与程序集身份冲突仍在加载前校验。

PluginStartupService.Remove(id) 只安排卸载，IsRemovalPending(id) 查询待删除状态；plugin-removals.json 与其他配置共用防抖、退出保存。Load 在发现插件前执行待卸载：Debug 只删除应用根目录内插件 DLL，Release 删除 plugins 下直接所属的独立插件目录。执行时重新验证绝对路径、插件 PE 标记，拒绝目录或文件链接；不允许删除应用根目录或宿主程序集。不存在的目标视为已删除，失败记录英文 Warning、保留请求以便后续启动重试。当前进程不卸载已加载代码、不撤销贡献，必须关闭并重新启动。重新构建会重新复制项目内示例插件，卸载验收应直接运行已构建 Launcher.exe。

不清除插件 Key 的启用选择，以便重新安装时保留原偏好。Debug/Release 共用用户配置，但卸载请求限定原绝对发布路径；另一发布目录启动不会删除该路径，记录失败并保留请求。
插件名称由 PE 内嵌 .resources 数据解析：按当前 UI 区域性及父区域性查找旁置卫星程序集，再回退插件中文中性资源；缺键回退名称键。资源读取不执行插件代码，不加载禁用插件；资源所有者必须位于插件程序集。稳定 Key 与入口全名不受翻译影响。
插件日志：来源 FrameworkApplication，消息 Loading plugin <Key>，不显示入口全名或路径。PrepareItemAsync 另传 serviceOwnerKey 给 ModuleServiceCatalog.Start(batch, ownerKey, isPlugin)；启动、正常停止及停止失败消息不显示服务类型全名，插件使用 Plugin service 和 Key，内置模块使用 Module service 和 ModuleName。StartedService 保存 OwnerKey 与 IsPlugin，确保失败回滚和退出停止使用同一标识。内置模块沿用 ModuleName；贡献批次名称、模块目录与依赖身份不变。示例输出 Plugin service started: sample，关闭为 Plugin service stopped: sample。
