# Services — 手动验证

不新增自动化测试。编译检查是基本卫生，验收由用户按 docs/agents/verification.md 本机运行应用确认。

    dotnet run --project Launcher/Launcher.csproj -c Debug

1. 控制台出现 Basic services started，启动台之后显示主页；“系统核心”列表包含 DigitalWorkstation.Core.Services。
2. 文件 > 首选项打开设置，改变语言后重启标记出现；立即重启后界面按新语言显示，改回再重启恢复。
3. 切换面板对齐、拖动分隔条后正常关闭并重开，布局保留；重置布局后关窗重开维持默认布局。
4. 帮助 > 关于可正常打开和关闭。
5. 文件 > 首选项 > 插件管理列出示例插件；改变下次启动加载选择并重启，状态栏和页面状态随之变化，重新启用恢复。
6. 正常退出时控制台出现 Basic services stopped；保存不因服务迁移丢失。

Release 构建后可运行 Output/Release/Launcher.exe 检查分类目录加载和插件共享清单。运行验收待用户确认。
## 模块附加服务手动验收

1. 启用 Sample 插件，运行 dotnet run --project Launcher/Launcher.csproj -c Debug。控制台先出现 Basic services started，再出现 Module service started: DigitalWorkstation.Sample.SampleModuleService (DigitalWorkstation.Sample.SamplePlugin)，主窗口状态栏出现“示例插件已加载”。
2. 正常关闭应用。控制台先出现同服务的 Module service stopped，再出现 Basic services stopped；每次进程内启动与停止各一次。
3. 在插件管理页禁用 Sample 并重启，新进程不出现 SampleModuleService 启动/停止日志，状态栏贡献消失。重新启用并重启恢复，验证禁用过滤早于服务实例化。
4. 用 -c Release 运行相同流程，检查独立插件目录下使用同一生命周期契约。

错误路径可临时修改 SampleModuleService.Start：先置 IsRunning=true，再抛 InvalidOperationException("Manual startup failure")。启动台应呈现失败，日志包含一次服务 stopped；选择继续后无 Sample 贡献，设置页可用。验证后恢复 Start 原实现并重新构建。

贡献准备失败路径可临时让 SampleStatusBarItem 构造抛英文异常：已启动的服务应先停止，然后进入失败决策；继续后不出现其状态栏项。验证后恢复构造实现。Stop 异常可同样临时在 Stop 末尾抛异常，退出仍须出现 Basic services stopped。以上是手动故障演练，不引入持久诊断开关或自动化测试。

多个服务的登记顺序与逆序停止通过真实模块的服务日志检查；当前 Sample 只有一个附加服务。编译与代码检查不等同于错误路径和多服务运行验收，用户确认前不宣称已通过。

## 插件名称、Key 与卸载文件

PluginDiscovery 在 PE 元数据中读取 PluginAttribute 的稳定 Key、资源类型与名称资源键，不为读取禁用项名称而加载程序集。PluginDescriptor.Name 保留入口全名，DisplayName 为显示名称，Key 为稳定配置标识；PluginStartupInfo.Id 为 Key、Name 为显示名称。旧入口全名配置通过 MigrateIdentity 迁移到 Key，已有 Key 值优先。候选 Key 与程序集身份冲突仍在加载前校验。

PluginStartupService.Remove(id) 只安排卸载，IsRemovalPending(id) 查询待删除状态；plugin-removals.json 与其他配置共用防抖、退出保存。Load 在发现插件前执行待卸载：Debug 只删除应用根目录内插件 DLL，Release 删除 plugins 下直接所属的独立插件目录。执行时重新验证绝对路径、插件 PE 标记，拒绝目录或文件链接；不允许删除应用根目录或宿主程序集。不存在的目标视为已删除，失败记录英文 Warning、保留请求以便后续启动重试。当前进程不卸载已加载代码、不撤销贡献，必须关闭并重新启动。重新构建会重新复制项目内示例插件，卸载验收应直接运行已构建 Launcher.exe。

不清除插件 Key 的启用选择，以便重新安装时保留原偏好。Debug/Release 共用用户配置，但卸载请求限定原绝对发布路径；另一发布目录启动不会删除该路径，记录失败并保留请求。
插件名称由 PE 内嵌 .resources 数据解析：按当前 UI 区域性及父区域性查找旁置卫星程序集，再回退插件中文中性资源；缺键回退名称键。资源读取不执行插件代码，不加载禁用插件；资源所有者必须位于插件程序集。稳定 Key 与入口全名不受翻译影响。
