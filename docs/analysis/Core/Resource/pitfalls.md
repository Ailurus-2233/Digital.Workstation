# Core/Resource — 不变量与陷阱

## 隐含不变量

1. **键名 ≡ 属性名 ≡ resx `data name`，三者必须逐字一致（区分大小写）。**
   `Language.cs:25-153` 的每个属性用 `Get(nameof(属性名))` 取键；`Language.resx` / `Language.en-US.resx` 的 `data name` 是字符串字面量。`nameof` 只保证 C# 侧一致，resx 侧没有任何编译期检查。`ResourceManager.GetString` 键查找大小写敏感——`SplashPhaseReady` 与 `SplashphaseReady` 是两个键。

2. **两个 resx 的键集合必须一一对应。** 当前各 27 条。en-US 漏一条不会报错：en-US 用户静默看到中文回退值。这是最隐蔽的"漏翻译"来源。

3. **`ResourceManager` 基名三要素绑定**：`Language.cs:12` 的字符串 `"DigitalWorkstation.Core.Resource.Language"` ≡ 项目根命名空间（`DigitalWorkstation.Core.Resource`，由项目路径/默认根命名空间推导）+ resx 文件名 `Language`（不含扩展名）+ resx 位于项目根目录。三者任一变化（重命名 resx、移动 resx 到子目录、改 csproj 根命名空间），基名就失配，首次访问抛 `MissingManifestResourceException`——且这个字符串字面量不会被重构工具更新。

4. **`Manager` 是模块级单例**（`private static readonly`，`Language.cs:11`）。`ResourceManager` 线程安全，本模块因此无线程亲和性要求——可在 UI 线程或后台线程任意调用。但**语言切换粒度是线程**：`GetString` 每次调用读 `CultureInfo.CurrentUICulture`，想换语言必须在调用线程上改 `CurrentUICulture`（且通常要在 UI 文案读取之前设置）；模块自身不提供运行时换语言 API。

5. **无成对调用、无生命周期**：无 `IDisposable`、无初始化方法，随时可用。

## 易错改法

| 看似合理的改法 | 为什么静默破坏 |
|---|---|
| 只改 `Language.resx` 加新键，忘了 `Language.en-US.resx` | 编译通过、中文正常；en-US 用户静默看到中文（回退），无任何报错信号 |
| 用 IDE 重命名重构属性名 | `nameof` 和 C# 调用点都跟着改，但 resx `data name` 不变 → 运行时该键缺失，界面显示键名（`?? key` 兜底） |
| 把 `Language.resx` 重命名为 `Strings.resx` 并把基名同步改成 `...Strings` 但只改了一处 | 基名失配 → `MissingManifestResourceException`，启动即崩 |
| 给 `Get` 加"键缺失就抛异常"的严格模式 | 违背 `Language.cs:15-16` 注释明示的设计意图（"键缺失时返回键本身，便于发现遗漏"），会把原本可视觉发现的漏配变成运行时崩溃 |
| 在 resx `<value>` 里放 `{0}` 占位符后指望 `Get` 格式化 | `Get` 只做查表不做 `string.Format`；占位符会原样显示在界面上。当前 27 条文案均无占位符 |
| 把某属性的 XML doc 注释改了但不改 resx `<comment>`（或反之） | 两份"用途文档"漂移，后人不知哪个是真相；现状两者逐条一致 |

## 历史踩坑（从代码证据读出）

- `Language.cs:15-16` 注释"键缺失时返回键本身，**便于发现遗漏**"——说明开发中实际遇到过漏配键的问题，特意选了"显示键名"这种可视觉发现的兜底，而不是 null/空串/异常。
- `Language.cs:7` 注释"C# 中的显示字符串**一律**经本类获取，**不直接硬编码**"——"一律"的措辞说明这是被强调过的团队规约，硬编码文案是被禁止的反模式；code review 时看到消费方代码里出现中/英文字面量 UI 文案即违规。
- 每条 resx `<comment>` 都标注了用途位置（如 `Language.resx:151`"启动台显示进度前的初始阶段文本"），说明作者刻意把 resx 当作文档维护；删除 comment 会降低可维护性。
- `DashBoardWindowViewModel.cs:24` 把 `Language.SplashStartingText` 用作字段初值——意味着 `Language` 在 ViewModel 构造期（可能很早、任何线程）就会被首次访问，其静态初始化路径上不能再引入可能失败的新依赖。
