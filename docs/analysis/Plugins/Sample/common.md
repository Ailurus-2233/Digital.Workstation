# Sample 插件

## 职责与入口

`Plugins/Sample` 是文件夹扫描加载插件的最小示例：`SamplePlugin` 实现 Prism `IModule` 并标注 Core/Abstractions 的 `[Plugin]`，由启动流程发现，不在宿主模块目录中显式声明。`RegisterTypes` 通过 `RegisterShellContribution` 登记 `SampleStatusBarItem`；`OnInitialized` 无额外行为。

状态栏项的稳定 Id 为 `sample.status`，排序为 30，位于内置启动台项之后。标题读取插件自有 `SampleResources.PluginLoaded`，中文为“示例插件已加载”，英文为“Sample plugin loaded”。贡献的准备、提交及失败隔离沿用 [Framework 的启动贡献批次](../../Core/Framework/common.md)。

## 资源与依赖

`Resources/SampleResources.cs` 与同名两份 resx 属于本插件；facade 使用共享 `ResourceText` 按资源 owner 类型查找。英文资源随插件生成 `en-US/DigitalWorkstation.Sample.resources.dll`，可用于观察 Release 下插件目录中的卫星资源加载。图标复用 `Icons.Ready`。

构建编排由仓库 `Build/` 统一负责。Debug 输出平铺在程序根目录；Release 插件入口、依赖描述与私有资源放在 `plugins/Sample/`。示例只显式引用宿主 Core 项目，不新增第三方包引用；传递依赖中未被共享清单排除的 DLL 仍随插件复制。它主要演示入口、贡献和卫星资源，不作为 native 或同名多版本依赖的验证样例。

## 修改与验证

新增贡献时在 `SamplePlugin.RegisterTypes` 使用 Framework 的对应注册扩展，保留 `sample.` 前缀以避免与其他模块冲突。新增文案需同步资源 facade 与中英文 resx；不要把插件私有文案加入 Core/Resource。

手动运行应用，确认状态栏出现中文标题；将应用语言改成英文并重启，应出现英文标题。关闭应用后临时移走插件 DLL（Debug）或整个 `plugins/Sample` 目录（Release），直接启动已有产物（不重新构建），应正常进入 Shell，且该状态栏项消失。恢复文件后再次启动，条目应恢复。

文件职责：`Sample.csproj` 声明共享依赖；`SamplePlugin.cs` 是唯一插件入口；`SampleStatusBarItem.cs` 是 Shell 贡献；`Resources/` 持有私有文案。样例不定义额外事件、持久化状态或插件间依赖。

## Services 拆分接线

基础服务实现现位于 Core/Services，经 Framework 的项目引用传递。贡献登记扩展使用 DigitalWorkstation.Core.Services.Contributions；UI 扫描、收集与布局状态机仍在 Framework。布局存储在 Services.Layout，PanelAlignment 在 Abstractions.Layout，SetPanelAlignmentEvent 在 Common.Events；重启入口为 Services.ApplicationRestarter。配置路径、语言设置 Id、插件上下文前缀和 Release 分类目录保持原规则。新项目入口见 Services 的模块认知文档。

## 附加服务示例

SamplePlugin.RegisterTypes 先 RegisterModuleService<SampleModuleService>，再登记 SampleStatusBarItem。新增 SampleModuleService.cs 实现 IModuleService，Start 设置 IsRunning，Stop 清除；OnInitialized 仍为空，不自行启动。

SampleStatusBarItem 构造注入同一服务单例，IsRunning=false 时抛英文异常，因此状态栏项出现意味着服务已在贡献准备前启动。Id、标题资源、图标和排序不变。目录统一记录 Module service started/stopped 日志，Sample 不重复打日志。

禁用插件后新进程既不实例化服务也不准备状态栏贡献；启用恢复。正常退出时服务停止日志先于 Basic services stopped。部分 Start 失败、贡献构造失败、Stop 失败的手动验证见 [Services/testing.md](../../Core/Services/testing.md)，结束后恢复原样例代码。
示例入口通过 [Plugin("sample", typeof(SampleResources), nameof(SampleResources.PluginName))] 声明资源名称与稳定 Key；PluginName 在中性资源为“示例插件”、en-US 为 Sample plugin；设置列表显示名称，启用偏好使用 Key。删除后的验收直接运行现有程序，重新构建会恢复示例插件文件。
插件日志：来源 FrameworkApplication，消息 Loading plugin <Key>，不显示入口全名或路径。PrepareItemAsync 另传 serviceOwnerKey 给 ModuleServiceCatalog.Start(batch, ownerKey, isPlugin)；启动、正常停止及停止失败消息不显示服务类型全名，插件使用 Plugin service 和 Key，内置模块使用 Module service 和 ModuleName。StartedService 保存 OwnerKey 与 IsPlugin，确保失败回滚和退出停止使用同一标识。内置模块沿用 ModuleName；贡献批次名称、模块目录与依赖身份不变。示例输出 Plugin service started: sample，关闭为 Plugin service stopped: sample。
