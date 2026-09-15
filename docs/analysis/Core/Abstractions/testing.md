# Abstractions — 验证方式

本模块没有测试项目或测试框架引用。接口、attribute 构造与元数据没有资源查找、归并或持久化逻辑；这些行为在 Framework 与 Settings 模块验收，不为字段透传或窗口泛型转发新增测试。

## 契约变更的验收边界

1. 汇总全部调用方后统一编译，确认必填资源 Type、设置分组 Id、菜单可空 PathTitle 的签名迁移完整。单独编译 Abstractions 不能证明消费者已迁移。
2. 用临时场景执行 Framework 的真实扫描/收集/建树代码，检查显式资源来源、菜单引用先到标题后到、设置稳定 Id 合并与隐式组回退；具体场景见 `../Framework/testing.md`。
3. 最后启动应用，检查语言重启后菜单、工具视图与设置页的显示；不能仅用编译通过代替可观察行为。

解决方案现有 `UnitTest/Framework/ShellLayoutStateResizeTests.cs` 只覆盖布局尺寸，不覆盖本地化契约。并行改动时由集成方待所有改动完成后统一验收，避免半成品引起伪失败。
