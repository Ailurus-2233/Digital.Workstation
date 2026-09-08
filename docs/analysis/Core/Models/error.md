# Models — 异常与排查

## 本模块自身抛出的异常

**无。** 本模块 10 个文件全部是纯声明（空事件子类、位置 record、枚举），没有任何方法体、属性逻辑或构造验证，不可能主动抛出异常。record 的编译器合成成员（构造、`Equals`/`GetHashCode`/`ToString`、`Deconstruct`、with 表达式拷贝）不做参数校验，构造 `new StartupProgress(default, null!, -1, -1)` 也不会抛。

## 错误处理路径

本模块是"错误的载体"而非处理者。与启动失败相关的完整错误路径：

1. **产生**：`Core/Framework/FrameworkApplication.cs` 启动序列第 86-88 行 `await Task.Run(() => moduleManager.LoadModule(module.ModuleName))` 抛出模块加载异常。
2. **捕获**：第 91 行 `catch (Exception ex)` 捕获，第 92 行 `Logger.Error(ex, ...)` 记日志。
3. **转换**：第 93-94 行把异常压扁成 `ModuleLoadFailure(module.ModuleName, i + 1, total, ex.Message)` 发布——**原始异常对象、堆栈、内部异常全部丢失，只留 `ex.Message` 字符串**。
4. **呈现**：`DashBoardWindowViewModel.OnModuleFailed`（第 53 行起）把 `ErrorMessage` 显示在启动台。
5. **决策传播**：用户点"继续/退出" → `StartupFailureActionEvent` → `WaitForFailureActionAsync` → 继续循环或 `Shutdown()`。

## 排查方式（按症状）

| 症状 | 看哪里 | 常见原因 |
|---|---|---|
| 启动台阶段文案不更新 | 发布点 `FrameworkApplication.cs` 第 74/85/104 行是否被执行；订阅点 `DashBoardWindowViewModel.cs` 第 19 行是否先于首次发布执行 | 订阅晚于首次发布（PubSubEvent 无粘性，错过的历史事件不重放）；`ThreadOption.UIThread` 下 UI 线程阻塞 |
| 启动台卡在失败界面、按钮无效 | `FrameworkApplication.WaitForFailureActionAsync`（第 118-125 行）在 await `StartupFailureActionEvent`；`DashBoardWindowViewModel` 第 72/81 行是否真的 Publish | 决策事件无人发布 → 启动序列永久挂起（无超时、无取消）；或按钮命令未绑定 |
| 点 SideBar 项后主视图不切换 | `OpenMainViewEvent` 负载 Id 与 `IMainViewContribution.Id` 是否**精确匹配**（字符串级契约） | Id 拼写/大小写不一致；订阅方（MainWindowViewModel 第 35 行）尚未构造就被发布 |
| 面板显隐菜单无效 | `ViewPanelMenus` 第 17/23/29 行 Publish 与 `MainWindowViewModel.TogglePanel`（第 278 行）之间的 `TogglePanelTarget` 分支 | 新增枚举成员后 switch 落入 `_` 分支（`TogglePanel` 的 `_` 兜底为 BottomPanel），表现成"点任何新面板都翻转底部面板" |
| 启动台模块名显示空白 | `StartupProgress.ModuleName` 是否为 null；`DashBoardWindowViewModel.OnProgress` 第 48 行的阶段判断 | 非 LoadingModules 阶段按约定 ModuleName 为 null，这是正常约定而非 bug |
| `PubSubEvent` 编译报错、找不到类型 | `Core/Models/obj/Debug/Models.GlobalUsings.g.cs` 第 6 行的 `global using Prism.Events;` 是否存在；Models.csproj → Common 的引用链是否完好 | 删掉了 Models.csproj 第 10 行的 Common 引用（源码零引用 Common 类型，但 Prism 引用经它传递）；Prism 包版本变更导致 build props 注入的 global using 变化 |

## 日志

本模块不打日志。相关日志在发布方：模块加载失败由 `FrameworkApplication.cs` 第 92 行 `Logger.Error(ex, ...)` 记录**完整异常**（Serilog，控制台 sink，见 Core/Common 的 Logger）——启动台 UI 只显示 `ex.Message`，排查模块加载失败应先看控制台日志拿堆栈，而不是只看启动台界面。
