# Core/Resource — 术语表

## 模块特有术语与缩写

| 术语 | 定义 | 首次出现位置 |
|---|---|---|
| **Language** | 本模块唯一公开静态类，全部 UI 文案的统一入口。不是"编程语言"，而是"界面显示语言文案" | `Language.cs:9` |
| **中性资源（Neutral resource）** | 不带区域性后缀的 `Language.resx`，内容为中文；`ResourceManager` 找不到匹配卫星程序集时的最终回退 | `Language.resx`（文件名无文化后缀即语义） |
| **卫星资源 / 卫星程序集（Satellite）** | 带区域性后缀的 `Language.en-US.resx`，编译产出 `en-US/Resource.resources.dll`，只含资源不含代码 | `Language.en-US.resx` |
| **键（Key）** | resx 条目的 `data name`，与 `Language` 属性名严格同名；`Language.Get(string key)` 的入参 | `Language.cs:17` |
| **键缺失兜底** | `Get` 的 `?? key` 行为：键查不到时返回键名本身，让遗漏在界面上可见 | `Language.cs:19`（注释 :15-16） |
| **`CurrentUICulture` 回退链** | `ResourceManager` 内置行为：请求的 UI 区域性 → 父区域性 → 中性资源。本模块的实际链：en-US → 中文 | `Language.cs:11-12`（由 `ResourceManager` 语义引入） |
| **`Manager`** | 模块内私有单例 `ResourceManager` 字段名，基名 `"DigitalWorkstation.Core.Resource.Language"` | `Language.cs:11` |

## 领域词汇（来自 resx 键与注释）

| 词汇 | 含义 | 代码位置 |
|---|---|---|
| **DashBoard / 启动台（Launch Pad）** | 应用的功能入口聚合页模块（`Modules/DashBoard`）。注意中英不直译：中文"启动台"，英文 "Launch Pad" | `Language.resx:65-68`、`Language.en-US.resx:65-68` |
| **Splash / 启动画面** | DashBoard 窗口在应用启动阶段显示的进度画面，有阶段（Phase）概念 | 键前缀 `Splash*`：`Language.cs:93-113` |
| **StartupPhase / 阶段** | 启动过程的分段：`CoreServices`（初始化核心服务）→ `LoadingModules`（加载模块）→ `Ready`（就绪），失败时 `Failed`（模块加载失败）。枚举本身定义在 `Core/Models`，本模块只持有其显示文案 | `Language.cs:98-113` |
| **Shell** | 宿主外壳（`Modules/Workstation`），预置导航项、菜单、面板 tab、状态栏项 | 多处注释，如 `Language.resx:63` |
| **SideBar / BottomPanel / AuxiliaryPanel** | shell 的三个可显隐面板区域，各有"切换"菜单项文案 | `Language.cs:68-78` |
| **导航项（NavigationItem）/ 菜单项（MenuItem）/ 面板 tab / 状态栏项（StatusBarItem）** | shell 贡献点的四种类型，每种都有 `Title` 属性需要本模块供文案 | 键名后缀：`NavigationTitle`/`MenuTitle`/`TabTitle`/`StatusReadyTitle` |

## 与同名通用概念的区别

- **`Language.Get` ≠ `ResourceManager.GetString`**：前者包装后者并加了 `?? key` 兜底，永不返回 null；后者键缺失返回 null、资源清单缺失抛异常。
- **本模块的"就绪（Ready）"有两个键**：`StatusReadyTitle`（状态栏常驻项，`Language.cs:118`）与 `SplashPhaseReady`（启动画面阶段名，`Language.cs:143`），中文值同为"就绪"但用途不同，不能合并。
- **`Resource`（项目/命名空间名）** 指"本地化文案资源"这一件事，不是泛指图片/图标等资源——图标路径由 `Core/UIPackage` 的 `Icons` 类负责（消费方代码中文案键与 `Icons.Xxx` 总是成对出现，如 `Modules/Workstation/Menus/FileMenus.cs:18` 的 `[MenuItem("MenuExitTitle", Order = 100, Icon = Icons.Exit)]`）。

## 类名 ↔ 业务概念对照

- `Language`（类）↔ "全部界面文案的字典"
- 27 个属性 ↔ 27 个具体 UI 位置的标题/文本（逐条对应见 api.md 表格）
- `Language.resx` ↔ 中文文案事实源；`Language.en-US.resx` ↔ 英文翻译层
