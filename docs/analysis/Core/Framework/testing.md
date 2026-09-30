# Framework — 验证方式

本仓库按 docs/agents/verification.md 采用本机桌面手动验收。编译是基本检查；一次性隔离诊断用于复现和排错，不作为自动化验收，也不新增测试项目。现有 UnitTest/Framework 历史测试不覆盖启动贡献批次、视觉宿主或文件生命周期。

## 启动命令

```powershell
dotnet build Launcher/Launcher.csproj -c Debug
dotnet run --project Launcher/Launcher.csproj -c Debug
```

## 本轮统一验收

第 1 项配置保存已获用户验收；第 2–4 项实现完成后一起检查：

1. 正常启动经过启动台进入主页，菜单、状态栏、Ctrl+P、面板快捷键和设置入口可用。Ready 前已完成贡献准备与呈现接线。
2. 打开设置页，切换面板显隐、四档对齐、拖动尺寸，再重置布局。当前设置页继续显示且主视图状态保持；回主页、再开设置正常；默认布局尺寸和对齐恢复。
3. 改变布局后退出重开，位置、选中与尺寸恢复。修改后立即退出或立即重启也保存；重置后等待超过 500ms，旧配置不会重新出现。
4. 设置语言切换到另一项出现重启提示，改回启动值提示消失；立即重启后语言生效且布局保存。页面项目与服务使用同一有效声明。

## 需要临时模块的故障验证

当前仓库没有内置工具视图或冲突设置，以下场景需临时诊断贡献，不向产品加入演示项：

- 模块先登记菜单/命令，宿主构造抛异常：应在启动台出现对应模块失败；继续后没有其任何贡献，独立正常模块可用。部分登记后抛异常同样处理；依赖失败模块的后继不加载。退出分支终止应用。
- 两个同 Id 设置声明使用不同默认值、分组和 RequiresRestart：首个生效；读取无关 Id 或后加载声明不改变已选声明；被丢弃项不生成空分组。本批构造器可读自己的声明，批次失败后其他查询不能再读到它。
- 两个工具视图分别默认位于 ActivityBar 和 AuxiliaryPanel：跨三个 Bar 移动、隐藏面板后移动、连续重置；旧宿主清空、单个 Control 只挂一个宿主、默认位置恢复，空面板不残留内容。四档对齐下重复检查。

## 本次诊断证据及边界

临时程序位于系统 Temp 的 workstation-architecture-diagnostic-20260919，引用实际构建程序集。已观察：失败工厂准备时抛出且只调用一次；失败/半注册贡献不可见；正常贡献保留；重置保留主视图 ID 和对象并清空空面板；缓存对象在全部属性通知时保持宿主唯一；配置往返顺序一致；设置默认值不随无关查询漂移；拒绝声明不留缓存。布局诊断未创建真实可视树，不能替代拖拽、焦点和视觉布局的人工验收。

配置并发诊断位于 workstation-persistence-diagnostic-20260919-080111：序列化暂停时 Delete 等待在途写入，旧回调不能重建文件；Dispose 保存末次值；序列化失败保留旧文件与待写快照，恢复后可重试。

改窗口管理、菜单跨屏、命令面板或本地化时，还须针对改动手动验证对应平台行为。Release 插件打包与加载的回归见下节。

## 插件加载手动回归

1. 运行 dotnet run --project Launcher/Launcher.csproj -c Debug；主窗口状态栏只出现一项“示例插件已加载”，设置页仍可正常打开。启动日志包含 Loading plugin 及根目录插件 DLL 路径。
2. 关闭应用后执行 dotnet build Launcher/Launcher.csproj -c Release，再直接启动 Output/Release/Launcher.exe；状态栏出现同一项，日志路径改为 plugins/Sample/DigitalWorkstation.Sample.dll。目录内保留 .deps.json 与 en-US 卫星资源。
3. 关闭应用，将 Output/Release/plugins/Sample 整个目录暂移到 plugins 之外，再直接启动既有 Launcher.exe（不要重新构建）；样例状态栏消失，内置模块正常。放回并重启后恢复。
4. 在设置页切换英文并重启，状态栏显示“Sample plugin loaded”，验证资源随插件查找。
5. 关闭应用，将 Sample 目录复制为同级 SampleCopy；直接启动后重复插件进入启动台失败决策，选择继续应正常进入主窗口且不显示重复贡献。退出后删除这份临时副本，恢复唯一插件再重启。
编译检查不等同于以上手动验收。私有依赖多版本/native 资产应使用真实携带这些依赖的插件在 Release 验证，当前 Sample 只演示入口、贡献与卫星资源。

## 插件启动配置手动验收（待用户确认）

1. `dotnet run --project Launcher/Launcher.csproj -c Debug`。打开文件 > 首选项 > 插件管理：示例插件显示已加载、启动时加载勾选，页面只显示名称和加载状态，不显示路径；Ctrl+P 可搜索首选项，打开设置后选择插件管理标签。
2. 取消示例插件勾选：条目提示重启后生效，本次仍显示已加载，主页与状态栏贡献仍存在。重新勾选：条目重启提示消失。
3. 再禁用并关闭后直接运行现有 Launcher.exe：主页无 SamplePlugin、状态栏无插件贡献；插件管理仍列出该项，状态为已禁用、复选框未勾选。日志出现英文 Skipping disabled plugin，启动进度不包含该插件。
4. 启用并关闭后直接运行现有 Launcher.exe：主页及插件状态栏恢复，管理页显示已加载且无待重启提示。取消勾选后立即正常退出，再启动，也应保留选择。
5. 关闭程序后手工将 `%AppData%/Digital.Workstation/plugins.json` 的示例入口全名设为 false，再启动应禁用；移走配置应恢复默认启用。临时写入损坏 JSON，启动应记录英文 Warning 并回退默认，恢复原配置后继续。
6. 用 `-c Release` 重复禁用、重启、重新启用，确认独立插件目录下行为一致。切到英文并重启，检查页面标题、加载状态、开关、空态及重启提示。Debug/Release 共用该用户配置，验证前记录并在完成后恢复原选择。

编译检查仅确认代码与 XAML 可编译；上述桌面交互仍需本机手动验收，不新增自动化测试。

## 基础服务项目拆分

窗口管理、配置写入、布局存储、贡献目录、设置目录与存储、插件发现及启动选择、ApplicationRestarter 已归 Services；本页相关行为是 Framework 使用这些服务的协议，源码归属与接口见 [Services/testing.md](../Services/testing.md)。Framework 显式 Register/Start 基础服务并在真实 Exit 停止，语言声明及设置 Id 不变。模块附加服务已接入加载后启动、失败清理与退出逆序停止。

## 模块附加服务接线

Framework 在模块/插件入口初始化后、贡献准备前调用 Services.ModuleServiceCatalog.Start；失败拒绝贡献批次并 Stop 同批服务。内置模块失败状态回到 NotStarted；依赖检查仍按 availableModules。

真实 Exit 先设置退出守卫、逆序 Dispose 模块服务目录，再 Dispose 基础服务，保证停止回调仍可使用基础配置。服务契约与登记方式见 [Services/api.md](../Services/api.md)，故障和运行验收见 [Services/testing.md](../Services/testing.md)。不回滚普通容器登记或模块入口自行产生的副作用。