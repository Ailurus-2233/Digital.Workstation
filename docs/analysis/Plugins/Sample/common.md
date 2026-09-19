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
