namespace DigitalWorkstation.Core.Abstractions.Services;

/// <summary>
/// 模块附加服务。构造与登记不启动工作；Framework 在模块初始化后、贡献准备前调用 Start。
/// </summary>
public interface IModuleService
{
    /// <summary>
    /// 准备服务状态，完成后才允许模块贡献对外可见。同模块服务按登记顺序启动。
    /// 在 UI 线程调用，应快速返回；持续工作由实现自行调度。
    /// </summary>
    void Start();

    /// <summary>
    /// 停止工作并释放服务拥有的资源。Start 部分执行后失败也会调用；
    /// 必须容忍未完全启动的状态，不依赖模块贡献仍可见。
    /// 在 UI 线程同步完成，避免等待只能由 UI 线程执行的任务。
    /// </summary>
    void Stop();
}