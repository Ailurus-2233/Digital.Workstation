## Git

Agent 不得主动执行 `git commit` 或 `git push`。只有人验收通过并明确指示后，才可以提交和推送。

## Agent skills

### Issue tracker

Issues 作为 markdown 文件存放在本 repo 的 `.scratch/<feature>/` 下。See `docs/agents/issue-tracker.md`.

### Triage labels

五个 canonical roles，label string 与 role name 相同（`needs-triage`、`needs-info`、`ready-for-agent`、`ready-for-human`、`wontfix`）。See `docs/agents/triage-labels.md`.

### Domain docs

Single-context：repo root 下一个 `CONTEXT.md` + `docs/adr/`。See `docs/agents/domain.md`.
