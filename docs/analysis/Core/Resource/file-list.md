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

### `Language.cs`（114 行）

- 定义 `namespace DigitalWorkstation.Core.Resource` 下唯一公开类型 `public static class Language`。
- 关键成员：
  - `Manager`（:11-12）：私有静态 `ResourceManager`，基名 `"DigitalWorkstation.Core.Resource.Language"`。
  - `Get(string key)`（:17-20）：唯一取值入口，`GetString(key) ?? key`。
- 类头注释（:5-8）是模块规约，完整两句：①"界面文案的统一入口：按当前 UI 区域性读取语言资源（**中性资源为中文，en-US 为英文卫星程序集**）"；②"C# 中的显示字符串一律经本类获取，不直接硬编码"。

### `Language.resx`（137 行，19 个 `<data>`）

- **中性（无区域性后缀）资源 = 中文**，是 `ResourceManager` 的最终回退。
- 标准 resx 2.0 骨架：文件头四个 `resheader`（:49-60）——`resmimetype`（值 `text/microsoft-resx`）、`version`（值 `2.0`）、`reader`（完整值 `System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089`）、`writer`（完整值 `System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089`，即读写器类型都来自 System.Windows.Forms 程序集）；19 条条目（:61-136）。
- 每条 `<data>` 的完整结构：`<data name="键名" xml:space="preserve">`，子元素顺序固定为 `<value>`（中文文案）在前、`<comment>`（用途说明，与 `Language.cs` 属性 XML doc 一致）在后。`xml:space="preserve"` **逐条出现在全部 19 个 data 元素上**，保证文案首尾空白不被裁剪；同时文件头部 `xsd:schema` 里也有 `<xsd:attribute ref="xml:space" />` 声明（:16、:34）——两个层级都有。
- `xsd:schema` 骨架细节（:3-48）：先以 `<xsd:import namespace="http://www.w3.org/XML/1998/namespace" />` 导入 xml 命名空间；`data` 元素的 `name` 属性声明为 `use="required"`（必填），`type` 与 `mimetype` 为可选属性——全部 19 个 data 均未带 `type`/`mimetype`，即本模块资源全部是纯文本字符串，无二进制/文件引用资源；`value` 与 `comment` 子元素均声明 `minOccurs="0"`，即 value 可缺省/为空串（此时 `Get` 的 `?? key` 不触发，调用方拿到空字符串）。
- 三文件键序一致：`Language.cs` 的 19 个属性声明顺序与两个 resx 的 data 出现顺序完全一一对应（首键均为 `SettingsNavigationTitle`，末键均为 `SplashPhaseFailed`）——这是人工核对键集合的锚点；运行时 `ResourceManager` 按键名查找，与文件内顺序无关。
- 键集合与 `Language.en-US.resx` 完全对齐。

### `Language.en-US.resx`（137 行，19 个 `<data>`）

- en-US 卫星资源，编译为 `en-US/Resource.resources.dll`。
- 与中性 resx 同构、同键：**XML 声明、整段 `xsd:schema`、四条 `resheader`、19 个 `data name` 键名及 `xml:space="preserve"` 标注在两份文件中逐字相同、顺序一致；只翻译 `<value>` 与 `<comment>` 两个子元素**（运行时按键名查找，键名必须一致）；`<value>` 为英文翻译，`<comment>` 为英文注释。

### `obj/`

- `dotnet restore`/构建生成的 NuGet 与编译中间文件（`project.assets.json`、`Resource.csproj.nuget.g.props` 等）。不属于源码，本模块深读不覆盖其内容；遇到构建怪事可整体删除后重新 restore。
