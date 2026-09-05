# 领域文档

Engineering skills 探索 codebase 时，应如何消费这个 repo 的领域文档。

## 探索前先读

- repo 根目录的 **`CONTEXT.md`**
- **`docs/adr/`**——读取与你即将处理区域相关的 ADR

如果这些文件不存在，**静默继续**。不要标记缺失；不要提前建议创建。`/domain-modeling` skill 会在 terms 或 decisions 实际被解决时懒创建它们。

## 文件结构

Single-context 布局（本仓库采用）：

    /
    ├── CONTEXT.md
    ├── docs/adr/
    │   ├── 0001-<decision>.md
    │   └── 0002-<decision>.md
    └── src/

## 使用 glossary 词汇

当你的输出命名某个领域概念时（issue 标题、重构提案、假设、测试名），使用 `CONTEXT.md` 中定义的术语。不要漂移到 glossary 明确避免的同义词。

如果你需要的概念还不在 glossary 中，这是一个信号：要么你正在发明项目没有使用的语言（重新考虑），要么确实存在缺口（为 `/domain-modeling` 记录）。

## 标记 ADR 冲突

如果你的输出与现有 ADR 矛盾，明确指出，而不是静默覆盖：

> _Contradicts ADR-0007 (…)——但值得重新讨论，因为…_
