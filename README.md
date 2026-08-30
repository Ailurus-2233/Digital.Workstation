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
	- DashBoard：启动台（DashBoardWindow 进度窗）：显示核心服务初始化与逐模块加载进度（模块名 + i/N），模块失败时提供"继续（跳过）/退出"；同时向 shell 贡献导航项、面板 tab 等

## 目录结构说明

- Build/：统一的 MSBuild 配置（输出目录、依赖 DLL 归档/裁剪等）
- Core/：跨模块共享的“核心库”（抽象、框架、模型、基础设施）
- Modules/：主应用与业务/功能模块（模块指 Prism IModule；Workstation 是主应用，不是模块）
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
	 - 抑制 Prism 同步 InitializeModules 一次性加载，模块改由启动序列逐模块异步加载（ADR-0004）
5. 启动序列（OnFrameworkInitializationCompleted，ADR-0004）
	 - 初始化核心服务：登记主窗口、显示启动台（DashBoardWindow）、校验模块目录
	 - 逐模块异步加载：逐模块发布 StartupProgressEvent（阶段名 + 模块名 + i/N）；单模块失败时发布 ModuleLoadFailedEvent，启动台显示错误并经 StartupFailureActionEvent 回报"继续（跳过该模块）/退出"决策
	 - 就绪：启动台自动关闭，MainWindow 设为桌面生命周期主窗口并显示，无需手动操作
6. 模块：WorkstationApplication.ConfigureModuleCatalog
	 - 注册 Prism 模块（当前：DashBoardModule）

## 构建与运行

前置：安装 .NET SDK 10（global.json 固定 10.0.101，允许 rollForward: latestMajor）。

- 还原：
	- dotnet restore Digital.Workstation.slnx
- Debug 运行（开发调试）：
	- dotnet run --project Launcher/Launcher.csproj -c Debug
- Release 构建（生成可分发的目录结构）：
	- dotnet build Digital.Workstation.sln -c Release

## 输出目录（Output/）说明

Build/Base.props 统一将输出写入 Output/$(Configuration)/。

- Output/Debug/
	- 偏向开发调试：按默认规则输出依赖与目标文件（便于 IDE 运行）
- Output/Release/
	- Launcher.exe / Launcher.dll：启动器输出通常在该目录根部
	- core/：Core 项目输出（DigitalWorkstation.Core.*）
	- modules/：Modules 项目输出（DigitalWorkstation.*）
	- libraries/：NuGet 依赖按“包名前缀”归类复制（例：Serilog/*）
	- runtimes/：保留 linux-x64 / osx / win-x64，其余运行时目录会被移除

说明：Release 下 Build/ManageDlls.targets 会将 NuGet 依赖归档到 libraries/，并删除输出目录根部的依赖 DLL，配合 Launcher/AssemblyLoader.cs 的动态解析实现更清晰的发布目录结构。

## 如何新增模块（最小步骤）

1. 在 Modules/ 下新增一个工程（建议引用 Core/Framework 与必要的 Core/* 项目）。
2. 实现 Prism 的 IModule（RegisterTypes / OnInitialized）。
3. 在 Modules/Workstation/WorkstationApplication.cs 的 ConfigureModuleCatalog 中 AddModule<YourModule>()。
4. dotnet build -c Debug 或 Release 验证模块加载。