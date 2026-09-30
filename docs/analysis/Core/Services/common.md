# Services — 基础服务入口

Core/Services 是独立基础服务项目，程序集为 DigitalWorkstation.Core.Services。只引用 Abstractions、Common，不引用 Framework。此文档依据本次源码同步，未执行 ABC 闭卷验证。

## 职责

- BasicServices 统一登记窗口管理、贡献目录、设置目录、配置持久化、布局存储、设置存储和插件启动选择。
- 窗口管理实现保留 FrameworkWindowManager 类名，但命名空间和文件已迁入 Services/WindowManager；双接口共享同一实例。
- ConfigurationPersistence 管理设置、布局和插件选择的 DebouncedJsonFile，500ms 防抖、快照写入、同目录临时文件替换、退出刷新与释放沿用原规则。
- SettingsService 与页面共享 SettingCatalog 的有效声明，不维护第二份声明缓存。
- ShellContributionCatalog 保存启动批次和贡献工厂，只有成功批次可见；Framework 负责扫描、逐批准备及最终 Shell 呈现。
- Plugins/ 提供发现、隔离依赖解析、启动选择和快照；Framework 负责按依赖编排加载与 Continue/Exit 决策。
- ApplicationRestarter 先保存所有配置，成功才启动新进程并正常退出。

## 启动与退出

Framework 初始化 IoC → BasicServices.Register → 登记 Framework 的设置声明 → 解析 BasicServices → 接入实际 Exit → BasicServices.Start → 应用已存语言 → 宿主登记 → 创建 Shell → 启动台和模块加载。

Start 一次加载设置与插件选择。构造布局持久化服务时登记其写入器，不提前读取布局；布局读取仍在 Shell 准备时。Start 重复调用不重复加载，Dispose 后禁止再启动。实际 Exit 调用 Dispose，统一保存和释放计时器；取消关窗不提前释放。

语言必须早于 Shell 构造和模块登记，所以基础服务在 Prism RegisterTypes 阶段启动；启动台的 CoreServices 进度负责后续主窗口登记和模块目录校验，不能把已存语言应用延后。

## 跨程序集协作

Services 的内部贡献批次、插件描述和发现入口仅向 Framework 授予 InternalsVisibleTo；保持内部协议，不把启动细节扩散给普通模块。模块登记贡献需要 using DigitalWorkstation.Core.Services.Contributions，扫描扩展仍使用 Framework 的对应命名空间。

PanelAlignment 是共享契约，位于 Abstractions/Layout；SetPanelAlignmentEvent 位于 Common/Events。Services/Layout 的 DTO 只依赖契约，不依赖 Framework 的布局状态机；状态与存储 DTO 的转换仍在 Framework/ShellLayoutConfiguration。

## 模块附加服务生命周期

模块在 RegisterTypes 调 RegisterModuleService<TService>() 登记具体单例；服务实现 IModuleService，构造与登记不得启动工作。登记归属沿用贡献批次的 AsyncLocal；同实现类型不能跨批次重复登记，批次关闭后拒绝迟到登记。

Framework 按模块依赖顺序初始化入口 → 关闭登记 → 按登记顺序在 UI 线程调用服务 Start → 准备贡献 → 提交批次 → 标记模块可用。依赖其他服务的启动顺序由登记顺序明确表达；模块依赖仍由模块目录或 PluginAttribute 声明。OnInitialized 时附加服务尚未启动，不在其中消费已运行状态。

Start 抛异常：先拒绝批次，再逆序 Stop 当前批次已接管的服务，包括 Start 部分执行后失败的实例。构造失败的对象没有成功交到目录，不调用 Stop；因此构造只能保存依赖，资源获取必须放在 Start。后续未启动服务不会构造或停止。贡献准备失败同样逆序 Stop 当前批次，不停止此前成功模块。

真实 Exit：先逆序 Stop 全局仍在运行的模块服务，再 BasicServices.Dispose 保存并释放基础配置服务。Stop 前移除清理责任，每个实例最多一次 Stop；Stop 异常只记 Error，继续其他清理。退出期间不再启动或登记新服务，不再弹出模块失败决策。

普通 DI 登记、模块 OnInitialized 的自行启动副作用仍不参与回滚。需要清理的工作必须迁入 IModuleService，不直接在模块入口启动。生命周期是同步 UI 线程协议：Start 快速返回，后台持续任务由实现持有；Stop 不等待必须回 UI 线程完成的任务。

公开契约在 Abstractions/Services/IModuleService.cs，登记与目录在 Services/Lifecycle；批次级 Start/Stop 只向 Framework 开放。API 和使用限制见 api.md 与 pitfalls.md。

## 插件名称、Key 与卸载文件

PluginDiscovery 在 PE 元数据中读取 PluginAttribute 的稳定 Key、资源类型与名称资源键，不为读取禁用项名称而加载程序集。PluginDescriptor.Name 保留入口全名，DisplayName 为显示名称，Key 为稳定配置标识；PluginStartupInfo.Id 为 Key、Name 为显示名称。旧入口全名配置通过 MigrateIdentity 迁移到 Key，已有 Key 值优先。候选 Key 与程序集身份冲突仍在加载前校验。

PluginStartupService.Remove(id) 只安排卸载，IsRemovalPending(id) 查询待删除状态；plugin-removals.json 与其他配置共用防抖、退出保存。Load 在发现插件前执行待卸载：Debug 只删除应用根目录内插件 DLL，Release 删除 plugins 下直接所属的独立插件目录。执行时重新验证绝对路径、插件 PE 标记，拒绝目录或文件链接；不允许删除应用根目录或宿主程序集。不存在的目标视为已删除，失败记录英文 Warning、保留请求以便后续启动重试。当前进程不卸载已加载代码、不撤销贡献，必须关闭并重新启动。重新构建会重新复制项目内示例插件，卸载验收应直接运行已构建 Launcher.exe。

不清除插件 Key 的启用选择，以便重新安装时保留原偏好。Debug/Release 共用用户配置，但卸载请求限定原绝对发布路径；另一发布目录启动不会删除该路径，记录失败并保留请求。
插件名称由 PE 内嵌 .resources 数据解析：按当前 UI 区域性及父区域性查找旁置卫星程序集，再回退插件中文中性资源；缺键回退名称键。资源读取不执行插件代码，不加载禁用插件；资源所有者必须位于插件程序集。稳定 Key 与入口全名不受翻译影响。
插件日志：来源 FrameworkApplication，消息 Loading plugin <Key>，不显示入口全名或路径。PrepareItemAsync 另传 serviceOwnerKey 给 ModuleServiceCatalog.Start(batch, ownerKey, isPlugin)；启动、正常停止及停止失败消息不显示服务类型全名，插件使用 Plugin service 和 Key，内置模块使用 Module service 和 ModuleName。StartedService 保存 OwnerKey 与 IsPlugin，确保失败回滚和退出停止使用同一标识。内置模块沿用 ModuleName；贡献批次名称、模块目录与依赖身份不变。示例输出 Plugin service started: sample，关闭为 Plugin service stopped: sample。
