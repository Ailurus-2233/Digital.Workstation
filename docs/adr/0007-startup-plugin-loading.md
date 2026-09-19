# 启动时发现插件与 Release 私有依赖隔离

## 状态
已接受（2026-09-19）。

## 上下文
内置模块由宿主显式加入 Prism 模块目录。新增插件需要通过程序集发现加入应用，复用模块的注册、初始化与 Shell 贡献能力。Debug 继续使用平铺输出；Release 中插件及其私有依赖需要作为独立文件夹分发。现有 Launcher 按简单名全局复用程序集、Build 将依赖集中到 libraries，直接扩大其搜索范围会让不同插件借用彼此依赖。

## 决策
1. 源码 Plugins 与 Modules 并列。插件入口实现 IModule 并标记 PluginAttribute，每个插件 DLL 一个入口；Release 每个直接子目录容纳一个插件。
2. Debug 只扫描程序根目录的顶层 DLL。Release 只扫描 plugins 的一级插件目录，不以进程当前工作目录定位文件。普通非插件 DLL 不作为模块加载。
3. Release 为插件创建独立程序集加载上下文，私有 managed/native 依赖及资源从本插件目录解析。私有依赖缺失时不能借用程序根目录或其他插件目录。宿主 Core、Prism、Avalonia 等交互契约及 .NET 运行时共享；共享程序集名单由 Build/PluginSharedAssemblies.txt 同时供构建与运行时使用。
4. 插件彼此独立；可以通过 PluginAttribute.DependsOn 声明对内置模块名称的依赖。全部内置模块先加载，再加载插件；依赖失败的内置模块会使插件进入失败决策。插件间依赖、运行中热加载与卸载不属于此次能力。
5. 插件复用 RegisterTypes、OnInitialized、启动进度与贡献批次。发现出的非法入口、依赖缺失、初始化或贡献准备异常在启动台显示，由用户选择继续或退出。继续会隐藏失败批次的贡献，不回滚普通 DI 注册、事件订阅或插件自行产生的副作用。
6. 插件入口直接以发现出的 Type 在宿主容器构造，避免按程序集名称重新定位到错误上下文。ViewModel 约定从 View 实际所属程序集查询。有效插件加入模块目录，完成贡献准备后标记为 Initialized。
7. 现有 dotnet build / dotnet run Launcher 路径需能构建仓库内插件。构建依赖不建立宿主对插件的编译引用；启动仍通过扫描发现。

## 后果
- 增加插件无需修改宿主的显式模块声明；复制 Release 插件目录并重启即可发现。
- 相比所有插件加入宿主全局搜索路径，独立上下文支持私有托管依赖的同名不同版本，并保持公共契约的类型身份一致。
- Debug 平铺目录无法同时存放同名依赖的多个版本；依赖隔离场景必须以 Release 验证。
- 插件保留依赖描述与标准 runtime 资产布局，退出原有的统一依赖搬移/删除规则。
- 共享名单属于插件兼容边界：插件应使用宿主兼容版本。进程内加载不提供进程隔离，native 库自身的全局状态及其间接系统加载仍受平台规则约束。
- 采用额外标记而非扫描所有 IModule，避免将内置模块或仅作为依赖携带的模块误认为插件；不另建 IPlugin 生命周期。

参考：[.NET 插件教程](https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support)。
