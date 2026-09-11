# Core/Resource — 模块简述

## 模块做什么

`DigitalWorkstation.Core.Resource`（项目文件 `Core/Resource/Resource.csproj`）是 Digital.Workstation 桌面应用的**界面文案（UI 字符串）资源模块**。它把整个应用 C# 代码里所有需要显示给用户的字符串集中管理：中文作为中性资源放在 `Language.resx`，英文放在 `Language.en-US.resx` 卫星资源，对外只暴露一个静态类 `Language`（`Core/Resource/Language.cs`），消费方通过 `Language.属性名` 或 `Language.Get(key)` 按当前 UI 区域性（`CultureInfo.CurrentUICulture`）取到对应语言的文案。

模块头部注释（`Language.cs` 第 5-8 行）明确规定了它的定位：**"C# 中的显示字符串一律经本类获取，不直接硬编码"**。

## 核心设计逻辑

1. **中性资源 = 中文**。`Language.resx` 不带区域性后缀，是回退兜底资源；`Language.en-US.resx` 是 en-US 卫星资源。这意味着当 `CurrentUICulture` 不是 en-US（或找不到对应卫星程序集）时，`ResourceManager` 自动回退到中文，不需要任何代码干预。设计含义：团队母语是中文，中文文案是"事实源"，英文是翻译层。

2. **静态类 + 单例 `ResourceManager`，不走 DI**。`Language` 是 `public static class`（`Language.cs:9`），内部持有 `private static readonly ResourceManager Manager`（`Language.cs:11-12`），基名硬编码为 `"DigitalWorkstation.Core.Resource.Language"`、绑定 `typeof(Language).Assembly`。理由：文案是无状态全局查询，注入它没有可测试性收益；静态访问让消费方（如 `Modules/Workstation/MainWindowViewModel.cs:151` 的 `SettingsTitle => Language.SettingsNavigationTitle`）一行即可取值。`ResourceManager` 本身是线程安全的，因此本模块天然线程安全。

3. **键缺失返回键本身，不抛异常**。`Get(string key)`（`Language.cs:17-20`）的实现是 `Manager.GetString(key) ?? key`。`ResourceManager.GetString` 对缺失键返回 `null`，这里用 `?? key` 兜底——注释明确说明意图是"便于发现遗漏"：界面上直接显示出键名（如 `MenuExitTitle`），一眼能看出资源漏配，而不是空白或崩溃。

4. **强类型门面：`nameof` 保证键与属性同名**。33 个静态属性（`Language.cs:25-182`）每个都是 `=> Get(nameof(XxxTitle))`。`nameof` 让键名与属性名永远一致：重命名属性时编译器会跟着改键名表达式，但 **`.resx` 里的 `data name` 必须手动同步改**（见 pitfalls.md）。

5. **注释即真相**。每个 resx 条目的 `<comment>` 和 C# 属性的 XML doc 注释内容一致（如 `SettingsNavigationTitle` 的注释 "shell 预置\"设置\"导航项的标题"），说明该条文案用在哪个 UI 位置。这是本模块唯一的"用途文档"。

## 状态流转

本模块**无内部状态、无写入路径**，是纯查询：

```
消费方调用 Language.XxxTitle（或 Language.Get(key)）
    → Get(nameof(...))（Language.cs:17）
    → ResourceManager.GetString(key)（System.Resources，按 CurrentUICulture 查找）
        → 命中 en-US 卫星资源（Language.en-US.resx 编译产物）→ 英文串
        → 否则回退中性资源（Language.resx 编译产物）→ 中文串
        → 键不存在 → 返回 null
    → ?? key 兜底：null 时返回键名本身
    → 返回 string 给调用方（通常赋给 shell 贡献项的 Title 属性）
```

- 入口：任何 `Language.*` 静态属性或 `Language.Get(string)`。
- 处理：`ResourceManager` 首次访问时惰性加载并缓存资源集，之后纯内存查表。
- 输出：本地化后的 `string`；永不抛"键缺失"异常（但资源清单整体缺失时 `ResourceManager` 构造/首次访问会抛 `MissingManifestResourceException`，见 error.md）。
- 副作用：无。模块不修改任何状态，也不提供运行时切换语言的 API——切换语言完全依赖进程级 `CultureInfo.CurrentUICulture`（通常在启动前由宿主设置）。

## 常见修改场景

1. **加一条新文案**：三处必须同步改——
   - `Core/Resource/Language.resx`：加 `<data name="NewKey">` 中文条目（含 `<comment>` 说明用途）；
   - `Core/Resource/Language.en-US.resx`：加同名 `<data name="NewKey">` 英文条目；
   - `Core/Resource/Language.cs`：加 `public static string NewKey => Get(nameof(NewKey));` 属性并复制用途注释。
   漏加 en-US 条目不会报错，en-US 用户会静默看到中文（回退行为）。
   设置相关键（`SettingsGeneralGroupName`/`SettingsLanguageName` 及枚举成员显示名键，ADR-0006）走同一流程；其中枚举成员键按「设置项名称键 + 成员名」约定生成（如 `SettingsLanguageNameZhCN`），见 [README 场景 8](../../README.md)。

2. **改某条文案的措辞**：只改 `Language.resx` 的 `<value>`（中文）和 `Language.en-US.resx` 的 `<value>`（英文）。键名和 `Language.cs` 不动。例如把"启动台"改成"主页"：改 `DashBoardNavigationTitle` 的两个 `<value>`。

3. **重命名一个键**：改 `Language.cs` 中属性名（`nameof` 自动跟随）+ 两个 resx 的 `data name`。注意消费方代码也要跟着改——强类型属性调用（如 `Modules/Workstation/MainWindowViewModel.cs:151` 的 `Language.SettingsNavigationTitle`）用 IDE 重命名重构可同时覆盖，但菜单 Attribute 里的字符串键（如 `Modules/Workstation/Menus/HelpMenus.cs:17` 的 `[MenuItem("MenuAboutTitle", ...)]`）IDE 重构不会跟随，和 resx 一样必须手动同步。

4. **新增一种语言（如 ja-JP）**：新增 `Language.ja-JP.resx`，逐条翻译 33 个键；`Language.cs` 无需改动，`ResourceManager` 按 `CurrentUICulture` 自动选取。新语言的键可以只翻译一部分，缺失的键回退到中文。

5. **排查界面上出现了英文键名（如显示 "SplashPhaseReady"）**：说明该键在两个 resx 中都缺失，或 resx 里 `data name` 与 `nameof` 属性名不一致（拼写/大小写）。对照 `Language.cs` 属性名检查 `Language.resx` 的 `data name`。
