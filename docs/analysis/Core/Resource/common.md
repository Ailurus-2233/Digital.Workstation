# Core/Resource — 模块简述

## 职责与边界

本模块提供跨程序集的资源查找机制 `ResourceText`，以及真正跨模块共享的产品文案 `SharedResources.ProductName`。模块私有字符串与所属模块代码一起维护，不集中到 Core：Framework 的命令面板与常规设置、DashBoard 的启动进度、Settings 的重启提示、Workstation 的 shell/主页文案分别由各自 `Resources/` 目录持有。

## 查找流程

`Owner.Key` → `ResourceText.Get(typeof(Owner), nameof(Key))` → 按 Type 缓存的 `ResourceManager` → `CurrentUICulture` 对应卫星资源 → 父区域性 → 中文中性资源。缓存的是 manager，不是已翻译字符串；同一键在不同 owner 中互不影响。缺键返回完整键名，资源清单缺失则仍抛异常。

资源 owner 是 public static class，和同名 `.resx`、`.en-US.resx` 同目录。`new ResourceManager(resourceType)` 使用类型全名作为基名、类型程序集作为资源程序集；不扫描所有模块、不按键猜测所有者、不回退到别的 owner。

## 声明与标识

有显示文案的贡献 attribute 显式携带 `typeof(Owner)` 和 `nameof(Owner.Key)`，注册端负责查找显示文本。菜单 `Path` 是稳定 slash-delimited ID 路径，`PathTitle` 是已解析的终端节点标题；建树器不查资源。仅挂接已有菜单时使用 `[MenuGroup("shell.file")]`，不复制标题或引用 shell 资源；此时 ResourceType/TitleKey/PathTitle 为 null，表示不参与标题决议。缺少声明的节点先显示稳定 Id，后到的首个标题声明会补齐标题。设置组以稳定 `Id` 合并，组名与设置项名各自从贡献的资源 owner 查找；隐式组显示 Id。身份与显示资源键互不替代。

## 修改配方

- 新增私有文案：在所属模块 owner 的两份 resx 加同名条目（保留用途 comment），在 facade 加 `ResourceText.Get(typeof(Owner), nameof(Key))` 属性。完成标准：C#、XAML、attribute 均引用正确 owner，两个语言条目齐全。
- 修改措辞：仅改该 owner 两份 resx 的 value；不要改稳定菜单路径、设置组 Id 或持久化设置 Id。
- 重命名键：同步 facade 属性、两份 resx 的 data name、C#/XAML 与 attribute 的 nameof。编译不检查 resx 键名。
- 新增语言：在每个需要翻译的 owner 旁新增区域性 resx，并同步构建的 SatelliteResourceLanguages；未翻译项仍回退中文。

完整 owner 文件位置见 file-list.md，异常语义见 error.md。
