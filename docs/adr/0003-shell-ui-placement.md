# Shell UI 归属：chrome 归 Workstation，契约归 Abstractions，机制归 Framework

Workstation 将承接由各模块组成的 UI 核心面板（菜单栏、快速工具栏、左侧功能导航、主窗口内容、状态栏、底部/右侧分栏等）。决定：shell 的 chrome（MainWindow 的布局骨架，只划区域不实现内容）放在 `Modules/Workstation`；模块贡献的契约（区域名常量、菜单/工具栏贡献接口）放 `Core/Abstractions`；组合机制（区域适配、面板宿主、贡献收集）放 `Core/Framework`；面板内容由各模块自己实现并经 Prism RegionManager 注册进命名区域。

原因：Launcher 的职责边界到 `AppBuilder.Configure<WorkstationApplication>()` 为止，不应感知 UI；且输出布局按源目录分类（Launcher → 根、Core → `core/`、Modules → `modules/`），chrome 作为应用 UI 归属 Modules 语义正确。按"谁依赖谁"分层后，新增模块只引用 Abstractions/Framework，无需改动 Workstation。

## Considered Options

- **与 Launcher 同放根目录** —— 拒绝：模糊加载器与应用 UI 的边界，且产物会落到输出根与 Launcher.exe 混杂。
- **现在就拆独立 `Modules/Shell` 项目** —— 暂缓：面板尚为空，Workstation 装得下；等应用生命周期与 shell UI 真的成为两件事时再拆。

## Consequences

- 新增模块只依赖 `Core/Abstractions`（契约）与 `Core/Framework`（机制），不向 Workstation 添加代码。
- 面板组合采用 Prism RegionManager 命名区域，而非自研面板系统。
