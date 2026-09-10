using DigitalWorkstation.Core.Abstractions.Contributions;

namespace DigitalWorkstation.Core.Framework.Layout;

/// <summary>
///     一次工具视图拖拽落放（ADR-0002）：<see cref="ToolViewBar" /> 在 Drop 时把落点信息包装为本记录，
///     经 MoveCommand 发给 ViewModel，由 <see cref="ShellLayoutState.MoveTab" /> 做状态转换
/// </summary>
/// <param name="TabId">被拖拽的工具视图 Id</param>
/// <param name="TargetBar">落点 Bar</param>
/// <param name="Index">落点插入序号（按目标 Bar 移除前的列表计）</param>
public sealed record ToolViewMove(string TabId, ToolViewPlacement TargetBar, int Index);
