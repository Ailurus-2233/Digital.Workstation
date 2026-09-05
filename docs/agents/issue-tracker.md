# 工单跟踪：本地 Markdown

本仓库的 issue 和 spec 以 markdown 文件存放在 `.scratch/`（已被 .gitignore 排除，不进提交）。

## 约定

- 每个 feature 一个目录：`.scratch/<feature-slug>/`
- Spec 是 `.scratch/<feature-slug>/spec.md`
- 实现工单每个 ticket 一个文件：`.scratch/<feature-slug>/issues/<NN>-<slug>.md`，从 `01` 开始编号；绝不合并成一个 tickets 文件
- Triage 状态记录为每个工单文件顶部附近的 `Status:` 行
- 评论和对话历史追加到文件底部的 `## Comments` 标题下

## 当 skill 说「发布到 issue tracker」

在 `.scratch/<feature-slug>/` 下创建新文件（必要时创建目录）。

## 当 skill 说「获取相关工单」

读取引用路径处的文件。用户通常会直接传入路径或工单编号。

## Wayfinding 操作

供 `/wayfinder` 使用。**map** 是一个文件，每个 ticket 对应一个 **child** 文件。

- **Map**：`.scratch/<effort>/map.md`——Notes / Decisions-so-far / Fog body。
- **Child ticket**：`.scratch/<effort>/issues/NN-<slug>.md`，从 `01` 开始编号，body 中是问题。`Type:` 行记录 ticket 类型（`research`/`prototype`/`grilling`/`task`）；`Status:` 行记录 `claimed`/`resolved`。
- **Blocking**：顶部附近的 `Blocked by: NN, NN` 行。当它列出的每个文件都是 `resolved` 时，ticket 即为 unblocked。
- **Frontier**：扫描 `.scratch/<effort>/issues/` 中 open、unblocked 且 unclaimed 的文件；按编号第一个胜出。
- **Claim**：在任何工作开始前设置 `Status: claimed` 并保存。
- **Resolve**：把答案追加到 `## Answer` 标题下，设置 `Status: resolved`，然后向 `map.md` 的 Decisions-so-far 追加 context pointer（gist + link）。
