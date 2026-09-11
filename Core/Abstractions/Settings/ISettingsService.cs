namespace DigitalWorkstation.Core.Abstractions.Settings;

/// <summary>
///     设置值读写服务（ADR-0006 决策 3）：Framework 实现，启动时一次性把 settings.json 加载入内存。
///     读纯走内存——已修改取用户值，未修改取声明的默认值（默认值不是单独存储层）；
///     写 = 更新内存 + 防抖落盘 + 广播 SettingChangedEvent（事件契约在 Core/Models）。
///     另承载「重启后生效」判定（决策 7）：跟踪本次进程内的值是否偏离启动时的生效值
/// </summary>
public interface ISettingsService
{
    /// <summary>
    ///     读取设置值：<paramref name="settingId" /> 已修改返回用户值，未修改返回声明的默认值，
    ///     未声明（含持久化值反序列化失败回退后仍无声明）返回 <typeparamref name="T" /> 的默认值并记日志
    /// </summary>
    T? Get<T>(string settingId);

    /// <summary>
    ///     写入设置值：立即更新内存、防抖落盘 settings.json、广播变更事件；
    ///     对 RequiresRestart 的设置项，当前进程行为不变，下次启动生效
    /// </summary>
    void Set<T>(string settingId, T value);

    /// <summary>
    ///     本次进程内该设置项的值是否已偏离进程启动时的生效值（ADR-0006 决策 7）：
    ///     「重启后生效」项级标记与重启横幅的判定依据；改回启动值即恢复为 false。
    ///     调用方自行结合 SettingItemContribution.RequiresRestart 过滤——本服务不感知该元数据
    /// </summary>
    bool IsPendingRestart(string settingId);
}
