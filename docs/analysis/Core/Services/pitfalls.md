# Services — 不变量与陷阱

1. 不添加 Services → Framework 引用。布局 DTO 与 PanelAlignment 已分离；服务需要 UI 时通过 Abstractions 契约或 Framework 编排。
2. ISettingsService 与 SettingsService 必须解析到同一实例；注册实现单例后以接口工厂转发，避免启动时 Load 的实例和页面读取实例分裂。窗口管理双接口同理。
3. 框架设置声明先于基础服务启动，语言应用先于宿主贡献与 Shell 构造。不要扫描 SettingsService.Assembly 代替 Framework 设置声明程序集。
4. 基础服务 Load 只在 Start 调用；不要重新在插件服务构造函数读文件或二次调用 Load 覆盖启动快照。
5. Timer、FlushPending、Delete、Dispose 的文件锁覆盖提交；后台写入不得反向读取设置内存。退出保存只接真实 Exit，不接可取消 Closing。
6. ContributionBatch 可见性和关闭注册的锁规则保持原样；只向 Framework 开放内部协议，不在消费方绕过目录 Resolve<IEnumerable<TContribution>>()。
7. PluginLoadContext 读取嵌入资源的名字必须随程序集迁移；上下文前缀 DigitalWorkstation.Plugin: 保持不变，以继续与 Launcher 隔离规则对接。
8. Debug 的同名依赖仍只能有一个版本；Release 插件私有依赖由各插件目录解析，禁止加入宿主 PATH。
## 模块生命周期不变量

- 构造不分配必须清理的资源；失败构造没有可交给目录的实例。Start 获得资源，Stop 能清理部分启动。
- 不从 OnInitialized 自行调用 Start；目录在入口初始化完成后启动，每批只有一次。
- 同模块依赖服务先登记、依赖者后登记。跨模块需要声明模块依赖，不能靠容器先解析一个尚未启动的服务。
- 贡献准备必须在服务 Start 成功后；贡献失败也要 Stop，不能只清理 Start 的异常。
- 登记共享贡献批次的关闭锁。目录启动先关闭批次，再拿目录锁，避免与登记路径出现锁顺序反转。
- 清理先移除责任，再调用 Stop；每次重新查找待清理实例，兼容 Stop 触发退出的重入。Stop 异常不阻止后续清理。
- 实现业务接口时使用工厂别名指向 RegisterModuleService 登记的同一单例；另外 RegisterSingleton<IContract,Implementation> 可能产生未被启动的第二实例。
- 服务只在 UI 线程同步启动停止，不阻塞等待需要 UI 调度器才能结束的后台任务。后台任务与取消资源由服务自己持有。
- 不把普通 DI 注册或事件副作用误认为被回滚；失败批次不会从容器注销其类型，当前设计也不支持重新启动或热卸载。

## 插件名称、Key 与卸载文件

PluginDiscovery 在 PE 元数据中读取 PluginAttribute 的稳定 Key、资源类型与名称资源键，不为读取禁用项名称而加载程序集。PluginDescriptor.Name 保留入口全名，DisplayName 为显示名称，Key 为稳定配置标识；PluginStartupInfo.Id 为 Key、Name 为显示名称。旧入口全名配置通过 MigrateIdentity 迁移到 Key，已有 Key 值优先。候选 Key 与程序集身份冲突仍在加载前校验。

PluginStartupService.Remove(id) 只安排卸载，IsRemovalPending(id) 查询待删除状态；plugin-removals.json 与其他配置共用防抖、退出保存。Load 在发现插件前执行待卸载：Debug 只删除应用根目录内插件 DLL，Release 删除 plugins 下直接所属的独立插件目录。执行时重新验证绝对路径、插件 PE 标记，拒绝目录或文件链接；不允许删除应用根目录或宿主程序集。不存在的目标视为已删除，失败记录英文 Warning、保留请求以便后续启动重试。当前进程不卸载已加载代码、不撤销贡献，必须关闭并重新启动。重新构建会重新复制项目内示例插件，卸载验收应直接运行已构建 Launcher.exe。

不清除插件 Key 的启用选择，以便重新安装时保留原偏好。Debug/Release 共用用户配置，但卸载请求限定原绝对发布路径；另一发布目录启动不会删除该路径，记录失败并保留请求。
插件名称由 PE 内嵌 .resources 数据解析：按当前 UI 区域性及父区域性查找旁置卫星程序集，再回退插件中文中性资源；缺键回退名称键。资源读取不执行插件代码，不加载禁用插件；资源所有者必须位于插件程序集。稳定 Key 与入口全名不受翻译影响。
插件资源归属校验：同程序集 typeof 在 CustomAttribute blob 中可仅序列化类型全名、不带程序集限定名，这是合法格式。解析同时接受未限定和当前程序集限定格式，并核对资源类型实际存在于插件 TypeDefinitions；外部程序集或不存在的类型仍被拒绝。示例实际 PE 值为 DigitalWorkstation.Sample.Resources.SampleResources，不能仅因缺逗号判为外部资源。
