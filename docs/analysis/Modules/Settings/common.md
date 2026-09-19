# Settings — 模块入口与修改指南

本页依据当前源码同步编写，未执行 deep-read 的 ABC 闭卷验证。修改设置页、编辑器或设置主视图登记前先读本页；设置声明与保存机制另读 [Framework/common.md](../../Core/Framework/common.md)、[api.md](../../Core/Framework/api.md) 及 [ADR-0006](../../../adr/0006-attribute-settings-registration.md)。

## 职责与依赖

`Modules/Settings` 提供普通 MainContent 主视图：左侧分组列表、右侧设置编辑器，以及需重启修改的项级标记和横幅。当前编辑器只支持枚举下拉框；模块自身没有声明演示设置项。预置“常规 / 语言”归 Framework 声明，显示名称与选项使用贡献者的资源。

项目直接引用 Abstractions、Framework、Resource、UIPackage；不引用 Workstation。主视图 Id 使用共享契约 `WellKnownViews.Settings`，由 Shell 的导航动作发布 `OpenMainViewEvent` 打开。

## 注册与页面创建

1. `SettingsModule.RegisterTypes` 调 `RegisterShellContribution<IMainViewContribution, SettingsMainView>()` 登记主视图贡献，并单独 `Register<SettingsPageView>()` 登记视图。普通 DI 注册用于视图解析，贡献登记用于 Shell 收集，两者职责不同。
2. `SettingsMainView` 只提供 `Id=WellKnownViews.Settings` 与 `ViewType=typeof(SettingsPageView)`。启动序列在模块加载完成后，于 UI 线程通过 `ShellContributionCatalog.Prepare` 构造本批贡献并提交；失败批次的贡献不会进入 Shell。此处准备主视图元数据，不会提前创建设置页面。
3. 启动序列在 Ready 前调用宿主 `PrepareShell` 建立主视图索引；打开设置时，Shell 按 Id 查贡献、经容器解析视图，并缓存内容实例。页面的 `AutoWireViewModel` 按约定装配 `SettingsPageViewModel`。
4. ViewModel 构造时从 `ShellContributionCollector` 取得设置项及分组快照，选择首个分组并创建编辑器；之后切换分组会重建 `Items`，重新打开同一个缓存页面不会重新构造 ViewModel。

手写贡献须使用 `RegisterShellContribution`；直接注册 `IMainViewContribution` 不会进入贡献目录。贡献批次只控制贡献可见性，普通 DI 注册及其他模块副作用不随失败回滚。

## 设置声明的共同来源

`ShellContributionCollector.GetSettingItems/GetSettingGroups` 与 `SettingsService` 的声明查询共用 Framework 的 `SettingCatalog`。它每次解释当前可见批次的声明，不缓存尚未提交的模块声明：

- 设置项按 Id 精确比较，按登记顺序保留首个声明，再按 `Order`、名称键排序。页面的类型/重启元数据与值读取的默认值因此来自同一条有效声明；查询其他设置不会改换声明。
- 显式分组按稳定 Id 合并：保留首份名称和资源来源，`Order` 取最小值。只有有效设置项引用的未声明分组才会补出，名称直接显示 Id、位次为 0；被同 Id 去重淘汰的设置项不会额外制造分组。
- 设置值仍通过 `ISettingsService.Get<T>/Set<T>` 读写，Id 仍为持久化键。元数据不在本模块重新去重，不从声明锚点的静态属性读取值。

这是目录读取的实时性；页面仍持有构造时取得的贡献快照，不据此推导出运行期动态加载或页面自动刷新能力。

## 编辑与重启状态流转

`SettingGroupModel.Key` 保存稳定分组 Id；`Name` 按贡献的资源类型与键解析，隐式分组直接显示名称。`SettingItemModel` 提供名称、Id、重启标记，并根据贡献的 `ValueType` 通过反射调用泛型设置读写接口。

`EnumSettingItemModel` 从枚举值建立选项，显示键为“设置项名称键 + 枚举成员名”，资源来源仍为贡献者。构造时直接给选中字段赋值，避免初始读取变成一次写入；改选为空或与当前值相同则不写，否则调用设置接口保存。

`SettingChangedEvent` 到达时，页面只刷新对应项的重启标记和总横幅。需重启项的当前值偏离进程启动值时显示提示，改回启动值即撤销。编辑器选中值不会因外部写入自动更新；增加外部修改入口时须同时考虑显示同步。

点击“立即重启”调用 Framework 的 `ApplicationRestarter.Restart()`，先经 `ConfigurationPersistence.FlushPending()` 完成设置和布局保存，成功才启动新进程并退出当前进程；保存失败留在当前进程。页面不转换 `ISettingsService` 的具体实现、不自行控制防抖计时器。当前进程语言不热切换。

## 修改入口与限制

| 修改内容 | 入口与连带关系 |
|---|---|
| 主视图注册和 Id | `SettingsModule.cs`、`SettingsMainView.cs`；Id 与 Abstractions 的 `WellKnownViews.Settings` 保持一致 |
| 分组、编辑器选择、重启横幅 | `ViewModels/SettingsPageViewModel.cs`；当前非枚举类型记英文 Warning 后跳过 |
| 新增一种编辑器 | 新建 `SettingItemModel` 子类，补 `CreateItemModel` 分支和 `Views/SettingsPageView.axaml` 的 DataTemplate；值仍走设置接口 |
| 设置页布局与绑定 | `Views/SettingsPageView.axaml`；后置文件只调用 `InitializeComponent`，ViewModelLocator 依赖命名约定 |
| 重启标记、横幅、按钮文案 | `Resources/SettingsResources.cs`、`.resx`、`.en-US.resx` 同步修改；贡献者的分组、设置项、枚举资源留在贡献模块 |
| 默认值、重复 Id、隐式分组 | Framework 的 `SettingCatalog`，页面使用收集结果；避免在本模块添加第二份规则 |
| 保存和退出行为 | Framework 的 `SettingsService`、`ConfigurationPersistence` 与 `ApplicationRestarter`，见 Framework 文档 |

## 手动验证

按 [仓库验证约定](../../../agents/verification.md)，在仓库根运行 `dotnet run --project Launcher/Launcher.csproj -c Debug`。以下步骤未表示已通过：

1. 从“文件 → 首选项”或左下角设置按钮打开页面，确认有“常规 / 语言”及枚举选项；返回首页后重新打开仍能编辑。
2. 改语言，确认项级提示与顶部横幅出现、当前界面语言保持不变；改回启动时的值，确认两处提示消失。
3. 再改语言并点击“立即重启”，确认新进程按所选语言显示；核对分组、枚举与 Settings 自有重启文案分别正确本地化。
4. 涉及重复 Id、失败批次、保存故障时，沿 [Framework/testing.md](../../Core/Framework/testing.md) 的诊断和手动步骤验证；页面观察有效设置及分组，不在本模块保留演示声明。
