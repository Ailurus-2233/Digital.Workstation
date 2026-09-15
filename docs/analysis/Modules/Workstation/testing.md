# Workstation — 验证方式

## 本模块的测试在哪

**本模块没有任何测试项目。** 解决方案中唯一的测试项目是 `UnitTest/Framework/Framework.csproj`（xUnit 2.4.2 + Microsoft.NET.Test.Sdk 17.6.0，net10.0），它只测 `Core/Framework` 的 `ShellLayoutState.Resize` 一族转换（`UnitTest/Framework/ShellLayoutStateResizeTests.cs`，11 个 `[Fact]`），不引用、不测试 `Modules/Workstation` 的任何类型（Workstation.csproj 也不在它的 ProjectReference 里）。

## 怎么跑（对本模块相关的部分）

本模块改动的**数据检测**落点在 Framework 的 `ShellLayoutState`——本模块 `MainWindowViewModel` 的全部状态转换（SelectActivity/Toggle*/Activate*/Resize）都只是调用它，因此改布局语义后应跑：

```powershell
# 仓库根目录
dotnet test UnitTest/Framework
# 只跑布局状态测试
dotnet test UnitTest/Framework --filter "FullyQualifiedName~ShellLayoutStateResizeTests"
```

## 改完代码后的最小验证集

本仓库是纯桌面端项目，约定**不做 UI 自动化测试验收**（根 AGENTS.md"验证约定"：本机 `dotnet run` 启动应用手动验证）。本模块的 UI 面（View/ViewModel 绑定、窗口交互、拖拽手势、菜单渲染）按约定跳过单元测试；单元测试聚焦数据检测——状态读写、转换、校验、计算逻辑。

按改动面分：

| 改动 | 验证方式 |
|---|---|
| 布局状态语义（显隐、clamp、tab 激活拒绝） | 实际逻辑在 `ShellLayoutState`：`dotnet test UnitTest/Framework` 全绿（11 用例，检测点映射见 docs/analysis/Core/Framework/testing.md） |
| `MainWindowViewModel` 交互逻辑（贡献收集、缓存、命令转发、布局持久化接线） | 无单元测试覆盖；手动 `dotnet run` 冒烟：启动 → 点 ActivityBar 导航项（SideBar 展开/再点收起；底部段应只见 shell 内置"设置"导航按钮，点击应打开设置页主视图；钉住区为空）→ Ctrl+B/Ctrl+J/Ctrl+Alt+B → 拖三条分隔条（边界应停在 Min/Max）→ 视图菜单三个切换项 → 文件>退出、帮助>关于；持久化：改布局后退出重进应恢复 → 视图菜单 Layout 组"重置布局"后应回全默认且 %AppData%/Digital.Workstation/layout.json 被删除 |
| 工具视图 `[ToolView]` attribute 与菜单类 attribute（Id/Order/图标/文案/默认位置/分组位次） | 手动验证渲染位置与排序（ActivityBar 顶部段/钉住区/两个面板，[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）；Id 类问题看 error.md 排查表，条目缺席看注册日志（工具视图：非可实例化 `Control`/同程序集重复 Id 被 `ToolViewRegistration` 记 `Logger.Warning` 跳过；菜单：非法签名/空段路径被跳过） |
| MainWindow.axaml 样式/布局 | 纯视觉，启动目验 |
| 主页品牌与模块树 | `dotnet run --project Launcher/Launcher.csproj -c Debug`：顶部图标靠右、名称与副标题左对齐。左侧固定标题“已加载模块”，树内仅“系统核心”“自定义模块”两组，可折叠展开；分别只显示真实已加载核心程序集与已初始化 Prism 模块。右侧固定标题“模块说明”。点击叶子出现单选高亮，在右侧标题下显示模块名称；选择分组时只清空名称，两个固定标题始终保留。切换到设置再“回到主页”刷新清单并清空选择与名称；空组显示提示而非可选假模块。切换语言并重启检查标题，窄窗口下检查模块名称及右侧内容。 |

本次顶部布局调整已构建通过（保留既有 AVLN3001 警告），使用实际 `EmptyStateView` 在中文/en-US、900/640/420px 宽度下离屏渲染：名称与副标题起点一致，与图标右边缘间隔均为 24px，无横向溢出。当前会话的原生 RenderTimer 限制见 [资源验证记录](../../Core/Resource/testing.md)，原生桌面仍按上表人工验收；未新增永久测试。

模块树调整已构建通过（0 错误，既有 AVLN3001 警告）。临时 headless/Skia 入口运行真实 Launcher 启动链，在隔离用户配置下分别执行中文与 en-US：识别六个 Core 程序集和两个已初始化模块；指针选择、高亮迁移、右侧名称联动、根/分组折叠展开及设置页返回主页清空旧选择和标题均通过。已目验实际窗口 1280px 和 960px 截图，窄窗口树节点换行，不使用横向滚动。烟测发现并修复返回主页标题残留；未新增永久测试。原生桌面验收仍受上述 RenderTimer 限制，离屏验证不替代本机人工验收。

标题与缩进修正：两个固定标题放在灰色内容卡片外上方，共用标题行；树内仅保留两个分组。构建通过，真实启动链离屏烟测验证标题不在卡片内部、选中后说明标题不被模块名称覆盖、折叠分组不隐藏标题；实测父子节点头部缩进差为 12px。已目验中文选中状态截图，原生桌面验证限制不变。

主页 MVVM 改造后：`dotnet build Launcher/Launcher.csproj --no-restore --nologo` 为 0 警告、0 错误，历史 AVLN3001 已消除，未使用警告屏蔽。真实启动链离屏烟测确认 Prism 自动关联模型、六个核心程序集及两个已初始化模块、指针选择与右侧名称、设置页返回主页清空选择；直接以主页 avares URI 调用 AvaloniaXamlLoader.Load 成功创建视图。已目验改造后主页截图，外部标题和 12px 缩进保持不变。临时烟测已清理，未新增永久测试；原生桌面验证限制不变。

## 测试约定（若将来为本模块新增测试）

沿袭 `UnitTest/Framework` 的约定（详见 docs/analysis/Core/Framework/testing.md）：xUnit、`[Fact]`、命名 `方法_场景_期望`、AAA、无夹具无 mock；数据检测类逻辑（如把某段转换抽成纯函数）才值得测，View/ViewModel 绑定不测。`MainWindowViewModel` 逻辑上可测的部分（`OpenMainView` 的 Id 查找、缓存命中）需要 `IContainerProvider` 与 `IEventAggregator` 桩，现状无此先例。
