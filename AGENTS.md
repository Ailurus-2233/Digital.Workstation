# AGENTS.md

## Agent skills

### 日志语言

代码中通过日志系统输出的消息内容必须使用英文，包括结构化日志的消息模板；不得使用中文或其他语言。

### 工单跟踪

工单为本地 markdown 文件，存于 `.scratch/<feature>/issues/`（已被 .gitignore 排除，不进提交）。见 `docs/agents/issue-tracker.md`。

### 验证约定

纯桌面端项目：本机 `dotnet run` 启动应用手动验证，不做自动化测试验收，无硬件/仿真器前提。见 `docs/agents/verification.md`。

### 领域文档

Single-context 布局：根部 `CONTEXT.md` + `docs/adr/`，均在实际解决 terms/decisions 时懒创建。见 `docs/agents/domain.md`。

### 模块认知文档

deep-read 产出（经 ABC 闭卷验证，main @ 04cfd02）；总索引与跨模块场景见 `docs/analysis/README.md`。

**先读规则**：修改某模块的任何代码前，必须先读 `docs/analysis/{module-path}/` 下的文档，`common.md` 是入口，按任务性质再读 `api.md`/`error.md`/`pitfalls.md`/`testing.md`。

**同步规则**：开发新功能或修复 bug 时，提交前必须同步更新受影响模块的 `docs/analysis/` 文档：接口变了改 `api.md`，行为/流转变了改 `common.md`，异常变了改 `error.md`，文件增删改 `file-list.md`，测试变了改 `testing.md`，发现新坑补 `pitfalls.md`，引入新术语补 `glossary.md`。文档与代码不一致视为任务未完成。

**解决方案索引同步规则**：`Digital.Workstation.slnx` 的 `/_docs/` 是 `docs/` 的解决方案视图索引。新增或删除 `docs/` 下的任意文件时，必须同步在 `.slnx` 中新增或删除对应的 `<File Path="docs/..." />` 项，并保持其归属的 `/_docs/analysis/`、`/_docs/adr/` 或 `/_docs/agents/` 虚拟文件夹正确。完成标准：`docs/` 下每个实际文件都能在 `.slnx` 找到且每个索引路径都指向现有文件。

- 改贡献契约/窗口管理接口（`Core/Abstractions`）时读 `docs/analysis/Core/Abstractions/`
- 改日志/全局容器门面（`Logger`/`IoC`）时读 `docs/analysis/Core/Common/`
- 增改事件契约（`Core/Models/Events`）时读 `docs/analysis/Core/Models/`
- 增改界面文案或多语言资源时读 `docs/analysis/Core/Resource/`
- 改主题配色或图标常量时读 `docs/analysis/Core/UIPackage/`
- 改启动序列、Shell 布局状态机或窗口管理器时读 `docs/analysis/Core/Framework/`
- 改启动台模块（进度窗、DashBoard 贡献）时读 `docs/analysis/Modules/DashBoard/`
- 改主窗口 chrome、shell 预置贡献或面板交互时读 `docs/analysis/Modules/Workstation/`
- 改程序集/native 解析或发布布局时读 `docs/analysis/Launcher/`
