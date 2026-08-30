namespace DigitalWorkstation.Core.Models.Events;

/// <summary>
///     启动进度负载；仅 <see cref="StartupPhase.LoadingModules" /> 阶段携带模块名与 i/N
/// </summary>
/// <param name="Phase">当前阶段</param>
/// <param name="ModuleName">正在加载的模块名，非加载模块阶段为 null</param>
/// <param name="ModuleIndex">当前模块序号（从 1 开始）</param>
/// <param name="ModuleCount">模块总数</param>
public record StartupProgress(StartupPhase Phase, string? ModuleName, int ModuleIndex, int ModuleCount);
