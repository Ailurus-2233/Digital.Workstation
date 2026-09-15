# Core/Resource — 文件结构与功能

| 文件/目录 | 职责 |
|---|---|
| `Core/Resource/Resource.csproj` | net10.0 类库，无项目或包依赖 |
| `Core/Resource/ResourceText.cs` | Type → ResourceManager 并发缓存与唯一共享查找机制 |
| `Core/Resource/SharedResources.cs`、`.resx`、`.en-US.resx` | 唯一共享键 ProductName，namespace `DigitalWorkstation.Core.Resource` |
| `Core/Framework/Resources/FrameworkResources.cs`、`.resx`、`.en-US.resx` | 命令面板、常规/语言设置名与枚举成员名；namespace `DigitalWorkstation.Core.Framework.Resources` |
| `Modules/DashBoard/Resources/DashBoardResources.cs`、`.resx`、`.en-US.resx` | 启动台状态栏与启动进度；namespace `DigitalWorkstation.DashBoard.Resources` |
| `Modules/Settings/Resources/SettingsResources.cs`、`.resx`、`.en-US.resx` | 重启标记/横幅/按钮；namespace `DigitalWorkstation.Settings.Resources` |
| `Modules/Workstation/Resources/WorkstationResources.cs`、`.resx`、`.en-US.resx` | shell 菜单/命令/状态栏、设置导航、主页与关于标题；namespace `DigitalWorkstation.Workstation.Resources` |

每个表中 `.resx`/`.en-US.resx` 都与对应 C# 文件同基名、同目录。SDK 默认嵌入资源，Build/Base.props 统一设置 RootNamespace/AssemblyName；没有手写 manifest 覆盖。

旧全局 Language.cs 与两份 Language resx 已删除。obj/ 是构建中间产物，不是资源源文件。
