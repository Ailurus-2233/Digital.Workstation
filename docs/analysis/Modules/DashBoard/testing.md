# DashBoard — 验证方式

## 本模块的测试在哪

**没有。** 全仓唯一的测试项目是 `UnitTest/Framework`（UnitTest/Framework/Framework.csproj，仅含 `ShellLayoutStateResizeTests.cs`），它只测 Core/Framework 的 `ShellLayoutState`，**不覆盖 DashBoard 的任何类型**。`UnitTest/` 目录下没有 DashBoard 测试项目，`DashBoard.csproj` 也没有任何测试引用。

## 测试约定（仓库级，适用于将来为本模块补测试）

- 本仓库是纯桌面端（Avalonia），**约定不做 UI 自动化测试验收**：View/ViewModel 绑定、窗口交互不进单元测试。单元测试聚焦**数据检测**——数据读写、转换、校验、计算逻辑（参照 docs/agents/verification.md 与 Core 各模块 testing.md 的约定）。
- 既有测试框架为 xUnit（UnitTest/Framework 所用）；新测试项目放 `UnitTest/DashBoard/` 并加入 `Digital.Workstation.slnx` 的 `/UnitTest/` 文件夹。

## 本模块中"可测的数据逻辑"清单

若将来补测试，值得覆盖的纯数据点是 `DashBoardWindowViewModel` 的事件→属性转换（构造注入 `IEventAggregator`，可用 Prism 真实 `EventAggregator` 实例驱动，无需 mock 框架）：

1. `OnProgress` 的阶段文案映射（DashBoardWindowViewModel.cs:41-47）：发布 `StartupProgress(StartupPhase.CoreServices/LoadingModules/Ready, ...)` 后断言 `PhaseText` 等于对应 `Language.SplashPhase*`；发布未知 `StartupPhase` 值断言 `PhaseText` 保持原值。
2. `ModuleText` 条件清空（第 48-50 行）：`LoadingModules` 阶段 `ModuleText == "name（i/N）"`（全角括号，见 `FormatModuleText` 第 61 行）；其余阶段为空字符串。
3. 失败态转换（`OnModuleFailed` 第 53-59 行）：发布 `ModuleLoadFailure` 后 `IsFailed==true`、`PhaseText==Language.SplashPhaseFailed`、`ErrorMessage==failure.ErrorMessage`；随后再发 `StartupProgress` 断言 `IsFailed` 复位为 `false`（第 40 行）。
4. 决策回传（`Continue`/`Exit` 第 70、79 行）：订阅 `StartupFailureActionEvent` 后执行 `ContinueCommand`/`ExitCommand`，断言收到对应 `StartupFailureAction`。
5. 贡献声明的声明值：`DashBoardStatusBarItem` 的属性矩阵（`Id="dashboard.status"`、`Title=Language.DashBoardNavigationTitle`、`IconPath=Icons.DashBoard`、`Order=20`，DashBoardStatusBarItem.cs:11-19）纯声明断言（价值低，防误改排序约定时才有意义）。

## 当前的验证方式（手动冒烟）

改完本模块后的最小验证集是**手动冒烟**（docs/agents/verification.md 约定的桌面端验证法）：

1. 启动应用（Workstation 宿主）：观察启动台（DashBoardWindow）依次显示"核心服务 → 正在加载模块 name（i/N）→ 就绪"，随后自动关闭并出现 MainWindow。
2. 人为制造一个模块加载失败：确认启动台显示错误详情与"继续/退出"按钮；"继续"跳过失败模块进入工作区，"退出"终止应用。
3. 工作区内：ActivityBar 顶部段与 BottomPanel 均无 DashBoard 条目（演示工具视图已删除）；状态栏出现"启动台"条目且位于"就绪"之后。
