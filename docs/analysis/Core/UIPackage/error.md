# UIPackage — 异常与排查

## 模块自身的错误处理策略：不处理

全模块 4 个文件中**没有任何 try/catch、返回值校验或空检查**。设计前提是：所有输入（颜色字面量、path 字符串、主题包清单）都是编译期写死的常量，错误只可能来自笔误，且会在启动期立即爆炸，无需防御。

## 可能抛出的异常

| 异常 | 触发条件 | 抛出位置 |
|---|---|---|
| `System.FormatException` | `Color.Parse` 收到非法颜色字符串（如十六进制位数错误、非法字符） | `VSCodePalette.Brush`（`VSCodePalette.cs:47`，`new SolidColorBrush(Color.Parse(color))`），由 `ApplyTo` 间接触发 |
| 主题包构造期异常（类型由第三方包决定） | 某个 `XxxTheme` 构造失败（通常是包版本不兼容、资源缺失） | `WorkstationTheme` 构造函数的 `Add(new SemiTheme())` 等四行（`WorkstationThemes.cs:12-15`） |
| `System.FormatException` / 解析异常 | const path 字符串不符合 StreamGeometry 标记语法 | **不在本模块抛出**——在消费方解析时抛出，如 `StreamGeometry.Parse(Icons.ChevronDown)`（`Modules/Workstation/MainWindowViewModel.cs:91`）或 `PathIcon` 绑定求值时 |

## 错误传播路径

- `ApplyTo` 与 `WorkstationTheme` 都在应用启动期被调用（`FrameworkApplication.Initialize`，`FrameworkApplication.cs:25-27`），任何异常都会中断启动、直接冒泡到应用入口——没有中间层捕获或转换。这符合"启动期 fail-fast"的意图：主题装不上时继续运行没有意义。
- 资源键本身不会"找不到而抛异常"：Avalonia 对缺失的 `DynamicResource` 键回退为 unset（控件显示透明/默认色），所以**拼错键名不会报错，只会静默失色**——这是本模块最容易踩的非异常型故障（见 pitfalls.md）。

## 排查方式

1. **启动崩溃、栈顶在 `Color.Parse`** → 检查最近一次改动的 `VSCodePalette.cs` 中 `Brush("#…")` 字面量格式（必须 `#RRGGBB` 或 `#AARRGGBB`）。
2. **启动崩溃、栈在某个 Theme 构造函数** → 核对 `UIPackage.csproj:10-15` 的包版本组合：Semi.Avalonia 系三包必须同版本（当前均 11.3.7.3），Ursa 两包同版本（当前均 1.15.1），且与 Avalonia 11.3.20  ABI 兼容。
3. **界面某区域颜色不对但没报错** → 先确认 `ApplyTo` 传入的是 `Application.Resources`（`FrameworkApplication.cs:27`）而非某个子级字典；再核对消费方引用的键名与 `ApplyTo` 写入的键名逐字符一致（如 `ChromeStatusBarBackground` vs 手误变体）。
4. **图标不显示或启动报 path 解析错** → 检查 `Icons.cs` 中被改动的 const 字符串是否破坏了 path 标记语法（命令字母 M/L/H/V/A/Z + 数字成对）。解析发生在消费方，栈帧会指向消费方文件而非 `Icons.cs`。
5. **改了颜色/图标但运行效果没变** → 若是图标且消费方在不同程序集，检查是否重新编译了消费方——`public const string` 会被内联进引用程序集（见 pitfalls.md）。
