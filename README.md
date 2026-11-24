# Digital.Workstation

使用 Avalonia + Prism 实现一个跨平台的多功能工具箱（模块化架构）。

## 技术栈

- .NET 10（见 global.json）
- UI：Avalonia Desktop（Fluent 主题、Inter 字体）
- 框架：Prism.Avalonia + DryIoc（DI / 模块化 / EventAggregator）
- MVVM：CommunityToolkit.Mvvm
- 日志：Serilog（控制台输出）

## 解决方案结构（Projects）

- Launcher：应用入口与启动引导（启动 Avalonia App、动态程序集解析）
- Core/
	- Abstractions：抽象层（如窗口管理 IWindowManager）
	- Common：通用基础设施（IoC 容器桥接、Logger、Events 共享事件契约与负载）
	- Services：基础服务实现（窗口管理、配置持久化、设置、插件发现与启动选择）
	- Framework：基础框架与启动编排（FrameworkApplication、Shell 布局、贡献扫描、MVVM 自动定位）
	- UIPackage：UI 资源包（聚合 Semi、Ursa 等第三方主题为统一的应用主题 WorkstationTheme）
- Modules/
	- Workstation：主应用（Avalonia Application + MainWindow），负责注册/加载模块
	- DashBoard：启动台（DashBoardWindow 进度窗）：显示核心服务初始化与逐模块加载进度（模块名 + i/N），模块失败时提供"继续（跳过）/退出"；另向状态栏贡献"启动台"条目

- Plugins/
	- MailReader：通过 [Plugin] 自动发现的邮件阅读插件，在主要侧边栏列出 IMAP 或 POP3 收件箱邮件。点击邮件，在以主题命名的主标签页中阅读正文；再次点击同一封邮件会回到已有标签。

## 目录结构说明

- Build/：统一的 MSBuild 配置（输出目录、依赖 DLL 归档/裁剪等）
- Core/：跨模块共享的“核心库”（抽象、框架、模型、基础设施）
- Modules/：主应用与显式声明的功能模块（模块指 Prism IModule；Workstation 是主应用，不是模块）
- Plugins/：启动时扫描发现的插件项目；每个插件沿用 IModule 生命周期并通过 [Plugin] 标记入口
- Launcher/：启动器（WinExe），负责启动 Avalonia + 预处理程序集加载
- Output/：编译输出目录（Debug/Release 会写入此处）
- Directory.Build.props / Directory.Build.targets：对整个解决方案生效的构建配置入口
- Digital.Workstation.slnx：解决方案文件

## 启动与模块加载流程（概览）

1. 入口：Launcher/Program.cs → Launcher.Initialize() / Launcher.Run(args)
2. 启动器：Launcher/AssemblyLoader.cs
	 - Release 下会预加载关键 DLL，并注册 AssemblyResolve，按目录（core / libraries / modules / runtimes）动态解析依赖
	 - Debug 下当前实现会跳过该流程（由编译条件控制），依赖默认探测
3. AppBuilder：Launcher.BuildAvaloniaApp() 指向 Modules/Workstation/WorkstationApplication
4. 框架层：Core/Framework/FrameworkApplication<TWindow>
	 - 初始化 IoC（DryIoc/Prism 容器桥接）
	 - 注册 IWindowManager（FrameworkWindowManager）
	 - 配置 ViewModelLocator：按 Views ↔ ViewModels 的命名/目录约定自动绑定
	 - 抑制 Prism 同步 InitializeModules 一次性加载，模块改由启动序列逐模块异步加载（[ADR-0004](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0004-startup-sequence.md)）
5. 启动序列（OnFrameworkInitializationCompleted，[ADR-0004](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0004-startup-sequence.md)）
	 - 初始化核心服务：登记主窗口、显示启动台（DashBoardWindow）、校验模块目录
	 - 逐模块异步加载：逐模块发布 StartupProgressEvent（阶段名 + 模块名 + i/N）；单模块失败时发布 ModuleLoadFailedEvent，启动台显示错误并经 StartupFailureActionEvent 回报"继续（跳过该模块）/退出"决策
	 - 就绪：启动台自动关闭，MainWindow 设为桌面生命周期主窗口并显示，无需手动操作
6. 模块：WorkstationApplication.ConfigureModuleCatalog
	 - 显式注册 Prism 模块（当前：DashBoardModule、SettingsModule）；插件由框架扫描发现

## 构建与运行

前置：安装 .NET SDK 10（global.json 固定 10.0.101，允许 rollForward: latestMajor）。

- 还原：
	- dotnet restore Digital.Workstation.slnx
- Debug 运行（开发调试）：
	- dotnet run --project Launcher/Launcher.csproj -c Debug
- Release 构建（生成可分发的目录结构）：
	- dotnet build Digital.Workstation.slnx -c Release

Launcher 自动为 Plugins/**/*.csproj 建立只参与构建的项目引用，因此 dotnet run 或 IDE 启动 Launcher 时也会构建插件；这些引用不把插件作为宿主的编译引用或运行时依赖。Release 也可单独执行 dotnet build Launcher/Launcher.csproj -c Release，再启动 Output/Release/Launcher.exe。

## 输出目录（Output/）说明

Build/Base.props 统一将输出写入解决方案根目录的 Output/$(Configuration)/；从解决方案或单独的 .csproj 构建都使用此路径。旧版本构建留在项目目录内的 Output/ 可直接删除。

- Output/Debug/
	- 宿主、模块、插件和依赖 DLL 平铺，插件发现仅扫描程序根目录的 DLL，并通过插件加载上下文解析私有依赖；同名多版本依赖隔离在 Release 验证
- Output/Release/
	- Launcher.exe / Launcher.dll：启动器输出通常在该目录根部
	- core/：Core 项目输出（DigitalWorkstation.Core.*）
	- modules/：Modules 项目输出（DigitalWorkstation.*）
	- plugins/<项目名>/：每个插件自己的入口 DLL、.deps.json、私有依赖 DLL、资源与 runtimes/ 资产
	- libraries/：宿主 NuGet 依赖按“包名前缀”归类复制（例：Serilog/*）
	- runtimes/：保留 linux-x64 / osx / win-x64，其余运行时目录会被移除

说明：Release 下 Build/ManageDlls.targets 只归档和清理宿主项目的依赖。Plugins 项目保留 SDK 生成的私有依赖布局，由独立加载上下文解析；Build/Plugins.targets 按 Build/PluginSharedAssemblies.txt 排除宿主共享框架及其 native 资产的重复复制。插件共用宿主的 .NET 运行时与共享契约，私有依赖从自身目录加载。

## 如何新增模块（最小步骤）

1. 在 Modules/ 下新增一个工程（建议引用 Core/Framework 与必要的 Core/* 项目）。
2. 实现 Prism 的 IModule（RegisterTypes / OnInitialized）。
3. 在 Modules/Workstation/WorkstationApplication.cs 的 ConfigureModuleCatalog 中 AddModule<YourModule>()。
4. dotnet build -c Debug 或 Release 验证模块加载。

## 如何新增插件（最小步骤）

1. 在 Plugins/<插件名>/ 下新增 net10.0 类库项目，引用 Core/Abstractions 与所需的 Core/Framework 等宿主项目。
2. 在入口 DLL 中声明唯一一个带 [Plugin] 标记的公开 IModule 实现；RegisterTypes / OnInitialized 与 Modules 中的模块一致。无需在 WorkstationApplication 中调用 AddModule。
3. 构建设置会自动启用 EnableDynamicLoading、依赖清单与私有依赖复制；如需在 IDE 解决方案树中展示项目，将其加入 Digital.Workstation.slnx 的 /Plugins/ 文件夹。
4. 执行 Debug 启动命令验证插件被发现；再执行 Release 构建并启动，验证 plugins/<插件名>/ 中的依赖和资源能正常加载。MailReader 加载成功后，主要侧边栏显示邮件阅读入口。

分发插件时复制整个 plugins/<插件名>/ 目录，保留 .deps.json 与 runtimes 子树；替换或移除插件后重启应用生效。共享清单是构建过滤和运行时共享的同一事实来源：第三方库使用宿主实际携带的精确程序集名，并列出共享栈的 native package ID；仅 Core 契约保留前缀规则。Serilog.Sinks.File 等宿主未携带的扩展仍是插件私有依赖。修改宿主插件契约或 UI 依赖边界时同步检查该清单。

主标签页的右键菜单由宿主提供「关闭」「关闭其他」「全部关闭」。页面可以在根控件上调用 `TabMenu.SetItems(this, actions)`，也可以在 XAML 根控件上绑定 `menus:TabMenu.Items="{Binding TabActions}"`（`menus` 对应 `DigitalWorkstation.Core.Framework.Menus`）。每个 `TabMenuAction` 指定标题、命令、组名、组序和项序，可选传入命令参数。同组使用相同的组名和组序，数值越小越靠前。每次打开菜单时读取该页面的配置。邮件插件的 `MailAccountView` 提供了完整示例。

可编辑页面可以在根控件上绑定 `layout:TabEditing.Editor="{Binding}"`（`layout` 对应 `DigitalWorkstation.Core.Framework.Layout`）。绑定对象实现 `IEditableTab`，通过 `HasUnsavedChanges` 的属性通知控制标签标题旁的圆点，用 `ClosePrompt` 提供本页的关闭提示，用 `SaveChangesAsync()` 保存草稿并返回是否成功。

每次编辑都要递增 `DraftVersion`。批量关闭时，若用户在确认放弃后再次编辑该页，宿主会就新版本重新询问。页面自带保存按钮时，通过 `PendingSave` 暴露正在执行的保存任务，宿主会等它完成再决定是否关闭。取消或保存失败会保留整批标签。重复打开有草稿或正在保存的文档会激活原标签并保留其编辑器。

## 邮件阅读（IMAP / POP3）

在设置的“邮件阅读”组选择 IMAP 或 POP3，填写企业提供的服务器、端口、TLS 方式和用户名，点击右下角的“保存”。插件不预设某一家服务商的地址。点击邮件侧边栏“账户与凭据”，输入密码／客户端授权码，可选择安全保存，然后手动刷新。应用启动时先读取缓存再自动刷新，运行期间每5分钟刷新一次；手动刷新与自动刷新不重叠。连接仅支持 TLS on connect 或必须成功升级的 STARTTLS，并使用系统证书验证。当前支持 IMAP / POP3 手动读取：取服务器顺序最近 100 封，再按邮件时间降序显示；IMAP 只读打开收件箱，不改变已读标记；POP3 使用 UIDL 稳定识别邮件，缺失或重复标识会明确报错。两种协议均不发送删除命令。切换协议后请手动填写对应端口和其他参数。HTML 正文转换为 Avalonia 原生阅读控件，保留段落、标题、粗体／斜体、列表层级、引用和表格文字；复杂布局按文档顺序简化。段落文字可选择复制，网页链接以实际地址按钮交给系统浏览器打开。只解码当前邮件的 cid 栅格图片，远程或本地路径图片显示占位，不加载外部资源或执行脚本。附件通过系统保存对话框选择位置；为避免覆盖，已存在的文件名会被拒绝，请选择新名称。

连接字段沿用 settings.json，密码不进入普通设置或日志。安全存储由 ICredentialStore 替换：Windows 使用当前用户 DPAPI 加密文件，macOS 使用系统 Keychain，Linux 使用 Secret Service 的 secret-tool（密码经标准输入传入，需系统提供该命令）。存储不可用、锁定或拒绝访问时界面明确提示会话模式，重启需重新输入。凭据按协议、服务器、端口、TLS 和用户名隔离，修改连接设置会取消请求并清空列表；若要保存新凭据，请再次点击安全保存。

本地缓存位于用户应用数据目录的 Digital.Workstation/mail-cache，每个连接身份独立保存最多100封正文、内嵌图片及附件元数据，凭据不进入缓存。离线重启可阅读缓存，未缓存附件需联网刷新后保存；用户另存文件不参与缓存淘汰。缓存写入先完成临时文件再替换旧快照，失败保留旧缓存并提示；插件停用或退出会停止轮询、取消在途请求。Windows 编译不能替代 macOS/Linux 的运行验证。首次从 Sample 升级时，构建会删除已知旧 Sample 入口产物，避免增量输出再次发现示例插件。

## Clash 本地代理

打开设置页的「Clash 代理」分组。内核管理行显示插件目录中的安装状态和本机内核版本，右侧可下载、更新或删除内核。每次启动工作站后第一次加载订阅时，插件从 MetaCubeX/mihomo 的 GitHub Releases 检查最新稳定版，在校验发布包的 SHA-256 后将内核安装到插件程序集旁。已有相同发布包时直接复用，网络不可用时可使用已安装的版本。删除会停止本地代理并保留订阅；再次下载后可继续使用。插件使用独立的内核进程和配置目录。

选中活动栏的「Clash 代理」展开侧栏，点击「节点列表」打开或切回对应的主标签页。列表展示节点的名称、订阅来源、最近一次订阅更新时间和通信延迟。侧栏点击「添加订阅」并保存链接；每张订阅卡片可刷新、编辑或删除，「全部刷新」会依次更新所有来源。按节点名或订阅名搜索，按来源和最近测速状态筛选，或按延迟排序。选择节点，点击「使用所选节点」，再启动代理。

HTTP 与 SOCKS5 默认共用 127.0.0.1:7897。端口可在设置页的「Clash 代理」分组修改，客户端自行指定代理地址。关闭侧栏后仍可使用代理；正常退出工作站时停止，下次启动恢复上次保存的状态。订阅链接和节点缓存保存在当前用户的 Digital.Workstation/Clash 配置目录中。
