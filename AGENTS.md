# AGENTS.md

## Agent skills

### 工单跟踪

工单为本地 markdown 文件，存于 `.scratch/<feature>/issues/`（已被 .gitignore 排除，不进提交）。见 `docs/agents/issue-tracker.md`。

### 验证约定

纯桌面端项目：本机 `dotnet run` 启动应用手动验证，不做自动化测试验收，无硬件/仿真器前提。见 `docs/agents/verification.md`。

### 领域文档

Single-context 布局：根部 `CONTEXT.md` + `docs/adr/`，均在实际解决 terms/decisions 时懒创建。见 `docs/agents/domain.md`。
