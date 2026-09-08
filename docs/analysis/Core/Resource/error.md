# Core/Resource — 异常与排查

## 模块可能抛出的异常/错误

### 1. `MissingManifestResourceException`（`System.Resources`）

- **触发条件**：`ResourceManager` 找不到基名 `"DigitalWorkstation.Core.Resource.Language"` 对应的资源清单。这不是"某个键缺失"，而是**整个资源文件没找到**。
- **抛出位置**：首次访问 `Language` 任何属性/`Get` 时（`Language.cs:11-12` 的 `Manager` 初始化或 `Language.cs:19` 的 `Manager.GetString(key)` 内部）。
- **常见原因**：
  1. 重命名了 `Language.resx` 文件名（基名随之改变，但 `Language.cs:12` 的字符串字面量不会自动跟随）；
  2. 改了项目根命名空间或把 resx 移进子文件夹（默认逻辑名 = 根命名空间 + 相对路径 + 文件名）；
  3. resx 的 Build Action 被从 `EmbeddedResource` 改掉。
- **排查**：`dotnet build` 后用 `Resource.dll` 反编译或 `strings` 确认内嵌资源名是否仍是 `DigitalWorkstation.Core.Resource.Language`；对照 `Language.cs:12` 的基名字符串。

### 2. `MissingSatelliteAssemblyException`（`System.Resources`）

- **触发条件**：`CurrentUICulture` 为 en-US 但 `en-US/Resource.resources.dll` 卫星程序集不在输出目录。
- **抛出位置**：`Manager.GetString(key)`（`Language.cs:19`）内部。
- **常见原因**：消费方项目（`Core/Framework`、`Modules/Workstation`、`Modules/DashBoard`）的输出目录拷贝不完整；手工裁剪了发布产物中的 `en-US/` 文件夹。
- **注意**：默认情况下 .NET 对缺失卫星程序集是**回退到中性资源（中文）而非抛异常**；只有显式配置 `<SatelliteResourceLanguages>` 且中性程序集标记 `NeutralResourcesLanguage(UltimateResourceFallbackLocation.Satellite)` 之类才会抛。本模块未设 `NeutralResourcesLanguage` 特性，因此实际行为是**静默回退中文**。

### 3. 键缺失——**不抛异常**（设计行为）

- `Get`（`Language.cs:17-20`）对缺失键返回键名本身。界面上显示英文键名（如 `SplashPhaseReady`）即是此错误的可见症状。
- **触发条件**：resx 两个文件都没有该 `data name`（拼写错误、漏加、重命名不同步）。
- **排查**：对照 `Language.cs` 的属性名逐一核对 `Language.resx` 与 `Language.en-US.resx` 的 `data name`（两者当前各 26 条，必须一一对应）。

## 错误处理路径

本模块**没有 try/catch**，不做任何异常转换或包装：

```
调用方 → Language.XxxTitle / Language.Get
    → ResourceManager.GetString
        ├─ 键缺失 → 返回 null → ?? key → 返回键名（兜底，非异常）
        ├─ 资源清单缺失 → MissingManifestResourceException 原样向调用方传播
        └─ 正常 → 返回本地化字符串
```

异常沿调用栈直接传播到消费方（如 `DashBoardWindowViewModel.cs` 的 splash 初始化）。模块自身假设：资源清单一定存在（编译期保证），所以只对"键缺失"这一最可能的人为失误做了兜底。

## 排查速查

| 症状 | 看哪里 | 常见原因 |
|---|---|---|
| 界面显示英文键名 | 该键在两个 resx 中的 `data name` 与 `Language.cs` 属性名是否一致 | resx 漏加条目 / 重命名只改了 C# 侧 |
| en-US 用户看到中文 | `Language.en-US.resx` 是否有该键；输出目录有无 `en-US/Resource.resources.dll` | 翻译漏配；卫星程序集未拷贝（静默回退） |
| 启动即 `MissingManifestResourceException` | `Language.cs:12` 基名 vs resx 文件位置/文件名 | resx 重命名或移动、根命名空间变更 |
| 某个值死活不更新 | resx `<value>` 改了但没重新编译；`obj/` 缓存 | 增量构建缓存（`Core/Resource/obj/`）可删了重建 |
