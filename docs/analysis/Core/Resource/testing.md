# Core/Resource — 验证方式

## 测试在哪

**本模块没有任何测试，整个解决方案目前也没有任何测试项目。** `Digital.Workstation.slnx` 只含 `Core/Models`、`Core/Resource`、`Core/UIPackage`、`Modules/*` 等源码项目，不存在 `*Tests*.csproj`。`Core/Resource/` 目录下也没有测试文件。

如实结论：本模块的正确性当前完全靠编译期检查（`nameof` 保证属性名合法）+ 运行时可见症状（键缺失时界面显示键名）来保证。

## 怎么跑

无可跑的测试命令。若将来补测试，按仓库纯桌面端约定：

- **UI 相关不做自动化测试验收**（View/ViewModel 绑定、窗口交互跳过单元测试）。
- **聚焦数据检测**：本模块唯一值得测的"数据逻辑"是资源查找与回退行为。

## 改完代码后的最小验证集（手工/将来自动化）

修改本模块后按以下数据检测点验证：

1. **编译**：`dotnet build Core/Resource/Resource.csproj` 通过（`nameof` 引用不出错）。
2. **键集合一致性**（最重要，可写成单元测试）：枚举 `typeof(Language)` 的 19 个公开静态属性，对每个属性名断言 `Language.Get(属性名) != 属性名`（即键在两个 resx 中都存在，没有走 `?? key` 兜底）。
3. **en-US 回退检测**：把 `CultureInfo.CurrentUICulture` 设为 `en-US`，断言每个属性返回英文值（如 `Language.MenuExitTitle == "Exit"`）；设为 `zh-CN` 或未识别区域性，断言返回中文值（如 `Language.MenuExitTitle == "退出"`）。
4. **缺失键行为**：`Language.Get("不存在的键")` 应返回 `"不存在的键"` 本身而非 `null`/异常。
5. **冒烟**：启动应用，肉眼确认 shell 菜单/面板/状态栏标题、DashBoard splash 阶段文案显示为目标语言而非键名。

## 测试约定（若新增测试项目）

- 框架：仓库尚无先例，按 .NET 主流建议 xUnit。
- 命名：建议 `Resource.Tests/LanguageTests.cs`；测试方法 `方法_条件_期望` 或仓库将来约定。
- 夹具：无需 mock——`ResourceManager` 查的是真实嵌入资源，直接测真资源即可；唯一需要在测试间隔离的状态是 `CultureInfo.CurrentUICulture`（用 `IDisposable` 夹具在测试后还原，避免污染其他测试）。
- 新增一条文案时对应的测试动作：无需为该条文案单独写测试——上面的"键集合一致性"测试通过反射自动覆盖新属性。

## 注意事项

- `CultureInfo.CurrentUICulture` 是线程级状态，测试并行运行时必须在各自线程设置，不能依赖进程全局。
- 改 resx 的 `<value>` 不改键名时，编译可能命中增量缓存，验证前必要时删 `Core/Resource/obj/` 重建。
