using DigitalWorkstation.Core.Abstractions.Services;

namespace DigitalWorkstation.Sample;

/// <summary>示例插件的运行状态；状态栏贡献只在服务启动后提供。</summary>
public sealed class SampleModuleService : IModuleService
{
    public bool IsRunning { get; private set; }

    public void Start() => IsRunning = true;

    public void Stop() => IsRunning = false;
}