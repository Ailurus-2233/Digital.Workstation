# Models — 验证方式

## 测试在哪

**本模块没有测试。** 仓库唯一的测试项目是 `UnitTest/Framework/Framework.csproj`，它既不引用 `Models.csproj`，也不涉及本模块任何类型（全仓 grep `Models|StartupProgress|TogglePanel` 于 UnitTest/ 目录零命中）。

无测试的原因与模块形态相符：10 个文件全部是纯声明——5 个无成员体的事件子类（机制在 Prism 的 `PubSubEvent<T>` 里，不是本仓库代码）、2 个编译器合成全部成员的 record、3 个枚举。本模块内**没有任何本仓库编写的可执行逻辑**可供断言；唯一可测的行为是 record 的值相等/`Deconstruct`/with 拷贝，而那是编译器合成语义，测它没有防御价值。

## 怎么跑（如需）

```powershell
dotnet test UnitTest/Framework/Framework.csproj
```

当前对 Models 的改动没有任何测试会被触发或需要筛选。

## 改完代码后的最小验证集

由于无单元测试覆盖，本模块的验证手段是**编译 + 消费方数据检测**：

1. **编译验证（最有效）**：`dotnet build Digital.Workstation.slnx`。本模块的改动几乎必然波及消费方调用点，而 C# 编译器会抓出所有失配：
   - 改 `StartupProgress`/`ModuleLoadFailure` 的位置参数 → `FrameworkApplication.cs` 第 74/85/94/104 行的 `new` 表达式编译错误（record 无默认值兜底）；
   - 改事件类名 → 全部 `GetEvent<T>()` 调用点编译错误；
   - 加 `StartupPhase` 成员 → `DashBoardWindowViewModel.OnProgress` 第 41-47 行的 switch 有 `_` 兜底**不会报错**，需人工检查（见下）。
2. **数据检测点（桌面端约定：不做 UI 自动化，聚焦数据）**：改完后手动启动应用，观察启动台显示的数据是否正确——阶段文案是否随 `StartupPhase` 切换、模块 i/N 序号是否从 1 起且与总数一致、制造一个模块加载失败后错误详情与"继续/退出"决策是否生效（继续则跳过该模块进工作区，退出则应用终止）。
3. **枚举扩散检查**：新增 `StartupPhase`/`TogglePanelTarget`/`StartupFailureAction` 成员后，grep 成员名所在枚举的全部 switch 消费点（`DashBoardWindowViewModel.OnProgress`、`MainWindowViewModel.TogglePanel`），凡带 `_` 兜底分支的都是编译放过的盲区；`TogglePanelTarget` 新成员还需在 `ViewPanelMenus` 补对应菜单方法。

## 测试约定（本仓库现状）

- 仓库约定：UI 相关（View/ViewModel 绑定、窗口交互）跳过单元测试，单元测试聚焦数据检测——数据读写、转换、校验、计算逻辑。本模块恰好是"纯数据定义"但无逻辑，落在约定两端之间：无 UI 可跳、也无逻辑可测。
- 若将来要为本模块新增测试（例如约定演化后给负载 record 加校验逻辑），应归入现有测试目录约定 `UnitTest/`（项目命名 `DigitalWorkstation.UnitTest.<X>`，由 `Build/Base.props` 第 44-47 行的 `_InUnitTest` 分支统一程序集名/输出路径），框架沿用 UnitTest/Framework 的既有选型。
