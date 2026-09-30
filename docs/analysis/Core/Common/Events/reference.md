# Common/Events — 模块关系

事件编译进 DigitalWorkstation.Core.Common.dll，由 Common.csproj 的 Prism 包提供 PubSubEvent 和 global using。不存在 Models.csproj 或 Models 程序集依赖。

Framework 直接引用 Common，发布启动事件；Services 引用 Common，发布设置事件。DashBoard、Workstation、Settings 经 Framework → Common 的传递引用消费事件；Launcher 经 Workstation 获得该引用。Abstractions 的注释可以描述事件协议，但不引用 Common，避免循环依赖。

Common/Events 包含 9 个事件类、3 个负载 record 和 3 个枚举。所有类型均为 public，文件清单见 file-list.md；事件内部没有状态和业务逻辑。