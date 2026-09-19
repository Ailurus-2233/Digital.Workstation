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
	- Common：通用基础设施（IoC 容器桥接、Logger 等）
	- Framework：核心框架层（FrameworkApplication、窗口管理实现、MVVM 自动定位等）
	- Models：跨模块共享的模型/事件（如启动进度 StartupProgressEvent）
	- UIPackage：UI 资源包（聚合 Semi、Ursa 等第三方主题为统一的应用主题 WorkstationTheme）
- Modules/
	- Workstation：主应用（Avalonia Application + MainWindow），负责注册/加载模块
	- DashBoard：启动台（DashBoardWindow 进度窗）：显示核心服务初始化与逐模块加载进度（模块名 + i/N），模块失败时提供"继续（跳过）/退出"；另向状态栏贡献"启动台"条目

- Plugins/
	- Sample：通过 [Plugin] 标记自动发现的示例插件，启动成功后向状态栏贡献插件已加载提示

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

Build/Base.props 统一将输出写入 Output/$(Configuration)/。

- Output/Debug/
	- 宿主、模块、插件和依赖 DLL 平铺，插件发现仅扫描程序根目录的 DLL；同名多版本依赖隔离在 Release 验证
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
4. 执行 Debug 启动命令验证插件被发现；再执行 Release 构建并启动，验证 plugins/<插件名>/ 中的依赖和资源能正常加载。示例 Sample 加载成功后，状态栏显示插件已加载提示。

分发插件时复制整个 plugins/<插件名>/ 目录，保留 .deps.json 与 runtimes 子树；替换或移除插件后重启应用生效。共享清单是构建过滤和运行时共享的同一事实来源：第三方库使用宿主实际携带的精确程序集名，并列出共享栈的 native package ID；仅 Core 契约保留前缀规则。Serilog.Sinks.File 等宿主未携带的扩展仍是插件私有依赖。修改宿主插件契约或 UI 依赖边界时同步检查该清单。
