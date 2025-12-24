# Digital.Workstation

使用 Avalonia + Prism 实现一个跨平台的多功能工具箱（模块化架构）。

## 技术栈

- .NET 8（见 global.json）
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
	- Models：跨模块共享的模型/事件（如 ShowMainWindowEvent）
	- UIPackage：UI 资源包（目前仅工程骨架，供后续沉淀通用样式/控件）
- Modules/
	- Workstation：主应用（Avalonia Application + MainWindow），负责注册/加载模块
	- DashBoard：示例模块（启动后先显示 DashBoardWindow，并可触发显示 MainWindow）

## 目录结构说明

- Build/：统一的 MSBuild 配置（输出目录、依赖 DLL 归档/裁剪等）
- Core/：跨模块共享的“核心库”（抽象、框架、模型、基础设施）
- Modules/：业务/功能模块（Prism IModule）
- Launcher/：启动器（WinExe），负责启动 Avalonia + 预处理程序集加载
- Output/：编译输出目录（Debug/Release 会写入此处）
- Directory.Build.props / Directory.Build.targets：对整个解决方案生效的构建配置入口
- Digital.Workstation.sln：解决方案文件

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
	 - 订阅 ShowMainWindowEvent：用于将 MainWindow 设为桌面生命周期的主窗口并关闭其他窗口
5. 模块：WorkstationApplication.ConfigureModuleCatalog
	 - 注册 Prism 模块（示例：DashBoardModule）

## 构建与运行

前置：安装 .NET SDK 8（global.json 允许 roll forward）。

- 还原：
	- dotnet restore Digital.Workstation.sln
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