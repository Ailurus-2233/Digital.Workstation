# Common/Events — 共享事件入口

事件契约属于 Common 项目，源码在 Core/Common/Events，命名空间为 DigitalWorkstation.Core.Common.Events。原 Models 项目已删除；此目录是 Common 的子模块文档，不是独立项目。

Prism 按类型身份撮合发布与订阅。所有消费方必须使用 Common 中的唯一定义，旧插件须重新编译。事件类只声明语义，机制由 PubSubEvent 提供；负载保持不可变 record 或枚举。

启动序列发布进度、模块失败，启动台订阅并回传 Continue/Exit。工作区发布打开主视图、返回主页、面板显隐和布局重置请求，Shell 处理。SettingsService 发布设置变更，设置页刷新重启标记。

修改事件签名时同时核对发布方与订阅方；新增枚举成员须核对 switch 默认分支。具体接口见 api.md，线程和订阅约束见 pitfalls.md。SetPanelAlignmentEvent 现位于 Common/Events；其负载 PanelAlignment 已成为 Abstractions/Layout 的共享契约。
SetPanelAlignmentEvent 由视图对齐菜单发布，MainWindowViewModel 订阅并更新窗口档位。设置变更的发布实现已迁入 Services，事件协议不变。
