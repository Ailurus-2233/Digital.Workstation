# Abstractions — 验证方式

## 本模块的测试在哪

**本模块没有任何测试。** `Core/Abstractions/` 下只有 12 个文件（1 个 csproj + 11 个 .cs），不存在测试项目、测试目录或以 `*Test*`/`*Tests*` 命名的文件（已通读模块全部文件确认）。模块内无任何测试框架（xUnit/NUnit/MSTest）引用。

> 解决方案级是否有其他测试项目引用本模块，超出本模块深读范围（约束：不读其他模块目录），待进一步调查。

## 为什么可接受

本模块是纯契约层：11 个 .cs 文件中 10 个只含接口/枚举/常量/attribute/元数据属性声明，唯一含方法体的 `WindowManagerExtenstion`（WindowManager/IWindowManagerExtenstion.cs）每个方法是一行 `manager.Xxx(typeof(TWindow))` 转发。可测试的数据逻辑（数据读写、转换、校验、计算）为零，符合桌面端「单元测试聚焦数据检测」的约定——这里没有数据逻辑可测。

## 怎么跑

无本模块测试可跑。改动本模块后的验证方式：

1. **编译验证**：`dotnet build Core/Abstractions/Abstractions.csproj`。本模块是解决方案最底层契约，任何签名改动会导致所有实现方/调用方编译失败——**编译错误即测试**，应进一步对整个解决方案 `dotnet build` 确认级联影响。
2. **数据检测点**：不适用（无数据逻辑）。若未来给 `WindowManagerExtenstion` 加了非平凡逻辑（参数校验、缓存等），才需要补测试。

## 改完代码后的最小验证集

- 本模块编译通过。
- 全解决方案编译通过（本模块接口被 shell 与各模块实现，签名变更的破坏面在编译期完全暴露）。
- 无需跑 UI 自动化（本仓库纯桌面端约定：不做 UI 自动化测试验收；且本模块不含任何 View/ViewModel）。

## 测试约定（若未来需要新增）

- 框架与命名：解决方案当前无先例可参照，待出现首个测试项目时确立；建议遵循 .NET 惯例 `DigitalWorkstation.Core.Abstractions.Tests` + xUnit。
- 可测对象候选：`WindowManagerExtenstion` 的转发行为（可用 mock `IWindowManager` 断言 `typeof(TWindow)` 正确传递）——但目前属「测转发=测实现细节」的低价值测试，不建议为覆盖率而加。
- UI 相关（窗口实际显隐效果）按仓库约定跳过自动化测试。
