# Core/Resource — 文件结构与功能

相对 `Core/Resource/` 的目录树：

```
Core/Resource/
├── Resource.csproj          项目文件
├── Language.cs              唯一 C# 源码
├── Language.resx            中性语言资源（中文，回退兜底）
├── Language.en-US.resx      en-US 卫星资源（英文）
└── obj/                     构建中间产物（NuGet restore 生成，勿手改、勿读作源码）
    ├── project.assets.json / project.nuget.cache / *.dgspec.json …
    └── Debug/ Release/      编译中间输出
```

## 各文件功能

### `Resource.csproj`（226B，8 行）

- `Microsoft.NET.Sdk` 类库项目；属性仅三条：`ImplicitUsings=enable`、`Nullable=enable`、`TargetFramework=net10.0`。
- **无任何 `<ProjectReference>`/`<PackageReference>`**——解决方案中最底层的叶子项目之一。
- 关键类型/入口：无代码；但它决定了 resx 的默认逻辑资源名（根命名空间 `DigitalWorkstation.Core.Resource` + 文件名）。

### `Language.cs`（168 行）

- 定义 `namespace DigitalWorkstation.Core.Resource` 下唯一公开类型 `public static class Language`。
- 关键成员：
  - `Manager`（:11-12）：私有静态 `ResourceManager`，基名 `"DigitalWorkstation.Core.Resource.Language"`。
  - `Get(string key)`（:17-20）：唯一取值入口，`GetString(key) ?? key`。
- 类头注释（:5-8）是模块规约，完整两句：①"界面文案的统一入口：按当前 UI 区域性读取语言资源（**中性资源为中文，en-US 为英文卫星程序集**）"；②"C# 中的显示字符串一律经本类获取，不直接硬编码"。

### `Language.resx` / `Language.en-US.resx`

- 中性资源为中文，`Language.en-US.resx` 为英文卫星资源。
- 两份文件键集合和顺序保持一致；每个条目均包含 `value` 与用途 `comment`。
- `ProductName`：中文「数字工作站」，英文 `Digital Workstation`，供应用名称、主窗口、启动台、主页和关于页使用。
- `AboutWindowTitle`：中文「关于 数字工作站」，英文 `About Digital Workstation`。
- 其余菜单、命令、状态栏、启动进度和设置文案继续按同一资源机制解析。

### `obj/`

- `dotnet restore`/构建生成的 NuGet 与编译中间文件（`project.assets.json`、`Resource.csproj.nuget.g.props` 等）。不属于源码，本模块深读不覆盖其内容；遇到构建怪事可整体删除后重新 restore。
