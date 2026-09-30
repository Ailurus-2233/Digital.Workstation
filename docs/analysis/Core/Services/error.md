# Services — 异常与排查

- 基础服务启动失败：Framework 的引导路径清理 BasicServices 后重新抛出，不伪装成可跳过模块。Start 发生在创建 Shell 前；先看启动控制台异常。
- settings.json 或 plugins.json 缺失/损坏：容错读取，缺失取默认，损坏记英文 Warning 后回退；读取不覆盖原文件。
- 配置写入失败：DebouncedJsonFile 记录英文 Warning，保留旧文件与待写快照；FlushPending 返回 false，立即重启被取消。
- 插件共享清单缺失：PluginLoadContext 抛 InvalidOperationException，检查 Services.csproj 的 EmbeddedResource LogicalName 与读取常量一致。
- 插件非法入口、重复身份或依赖解析失败：发现描述符保留错误，Framework 统一呈现 Continue/Exit。禁用项在加载程序集前过滤。
- 主窗口未登记或重复登记：FrameworkWindowManager 抛 InvalidOperationException；HandleMainWindow 必须在创建 Shell 后调用一次。ShowDialog 还要求主窗口已激活。
- 贡献迟到登记：已关闭批次抛英文 InvalidOperationException。拒绝批次后其贡献不可见，普通 DI 注册和模块副作用不因此回滚。
- 旧插件引用原 Framework 中的服务类型：需重新编译；不要复制旧类型或增加反向项目引用作为兼容方案。
## 模块服务错误

- 在模块初始化批次之外登记：RegisterModuleService 抛 InvalidOperationException，消息为 Module services must be registered during module initialization。
- 关闭登记后的异步任务继续登记：共用 ContributionBatch.Register 闸门，抛 Contribution registration ... is closed，尚未向容器插入该服务。
- 实现类型重复登记：抛 Module service ... is already registered，保留原登记。
- 构造或 Start 失败：当前模块进入既有 Continue/Exit 协议，失败批次贡献不可见；已接管实例逆序停止，原始失败仍作为模块错误显示。
- 贡献准备失败：Framework 先 Reject，再 Stop 同批服务；此前成功模块保持运行。
- Stop 失败：日志 Failed to stop module service 包含服务全名和模块名，继续其他服务；不把停止异常替换成原始启动错误。
- 已退出后再登记或启动：ObjectDisposedException，Framework 退出守卫阻止再次启动以及弹出失败决策。

成功日志为 Module service started: <类型> (<模块>) 与 Module service stopped: <类型> (<模块>)，归 ModuleServiceCatalog；日志必须英文。
插件资源归属校验：同程序集 typeof 在 CustomAttribute blob 中可仅序列化类型全名、不带程序集限定名，这是合法格式。解析同时接受未限定和当前程序集限定格式，并核对资源类型实际存在于插件 TypeDefinitions；外部程序集或不存在的类型仍被拒绝。示例实际 PE 值为 DigitalWorkstation.Sample.Resources.SampleResources，不能仅因缺逗号判为外部资源。
插件日志：来源 FrameworkApplication，消息 Loading plugin <Key>，不显示入口全名或路径。PrepareItemAsync 另传 serviceOwnerKey 给 ModuleServiceCatalog.Start(batch, ownerKey, isPlugin)；启动、正常停止及停止失败消息不显示服务类型全名，插件使用 Plugin service 和 Key，内置模块使用 Module service 和 ModuleName。StartedService 保存 OwnerKey 与 IsPlugin，确保失败回滚和退出停止使用同一标识。内置模块沿用 ModuleName；贡献批次名称、模块目录与依赖身份不变。示例输出 Plugin service started: sample，关闭为 Plugin service stopped: sample。
