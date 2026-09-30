# Common/Events — 异常与排查

事件声明自身不抛异常，也不记录日志。模块加载异常由 Framework 捕获并记录完整堆栈，ModuleLoadFailure 只携带错误文本；启动台呈现错误并发布用户决策。

找不到 PubSubEvent 时核对 Common.csproj 的 Prism 包还原与 global using。发布后无响应时核对双方是否使用 DigitalWorkstation.Core.Common.Events 中同一类型；旧 Models 插件必须重新编译。UI 回调异常到对应订阅方排查，启动台订阅使用 ThreadOption.UIThread。