using System.Text.Json;
using System.Text.Json.Serialization;
using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Models.Events;

namespace DigitalWorkstation.Core.Framework.Settings;

/// <summary>
///     设置服务（ADR-0006 决策 3/4）：%AppData%/Digital.Workstation/settings.json 的读/防抖写。
///     启动时经 <see cref="Load" /> 一次性加载入内存；<see cref="Get{T}" /> 纯内存读
///     （未修改时回退声明的默认值，默认值经容器中的 SettingItemContribution 惰性按 Id 缓存）；
///     <see cref="Set{T}" /> 更新内存 + 防抖落盘 + 广播 SettingChangedEvent。
///     读容错仿 LayoutPersistence：文件缺失/损坏一律按无修改处理，只记日志不打断应用。
///     默认值的容器解析假定 UI 线程调用（与 ShellContributionCollector 同约定）
/// </summary>
public sealed class SettingsService(IEventAggregator eventAggregator, IContainerProvider containerProvider)
    : ISettingsService
{
    /// <summary>
    ///     设置配置文件路径
    /// </summary>
    public static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Digital.Workstation", "settings.json");

    private const int DebounceMilliseconds = 500;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly Lock _gate = new();

    /// <summary>
    ///     用户已修改的值（落盘内容的全量内存镜像）
    /// </summary>
    private readonly Dictionary<string, JsonElement> _values = new(StringComparer.Ordinal);

    /// <summary>
    ///     设置项声明的惰性缓存：按 Id 缓存，缓存未命中时重新枚举容器
    ///     （启动早期模块设置项尚未注册，不能一次性定死）
    /// </summary>
    private readonly Dictionary<string, SettingItemContribution> _declared = new(StringComparer.Ordinal);

    private System.Threading.Timer? _timer;

    /// <summary>
    ///     启动时一次性加载 settings.json 入内存；文件缺失/损坏记 Warning 后按无修改处理
    /// </summary>
    public void Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return;
            }

            var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                File.ReadAllText(FilePath), SerializerOptions);
            if (values is null)
            {
                Logger.Warning($"设置配置为空，全部按默认值处理：{FilePath}", nameof(SettingsService));
                return;
            }

            lock (_gate)
            {
                foreach (var (key, value) in values)
                {
                    _values[key] = value;
                }
            }
        }
        catch (Exception exception)
        {
            Logger.Warning($"设置配置读取失败（{exception.GetType().Name}），全部按默认值处理：{FilePath}",
                nameof(SettingsService));
        }
    }

    public T? Get<T>(string settingId)
    {
        lock (_gate)
        {
            if (_values.TryGetValue(settingId, out var element))
            {
                try
                {
                    return element.Deserialize<T>(SerializerOptions);
                }
                catch (Exception exception)
                {
                    Logger.Warning(
                        $"设置项 \"{settingId}\" 的持久化值反序列化失败（{exception.GetType().Name}），回退默认值",
                        nameof(SettingsService));
                }
            }
        }

        var contribution = FindContribution(settingId);
        if (contribution is null)
        {
            Logger.Warning($"读取未声明的设置项 \"{settingId}\"，返回默认值", nameof(SettingsService));
            return default;
        }

        return contribution.DefaultValue is T value ? value : default;
    }

    public void Set<T>(string settingId, T value)
    {
        if (FindContribution(settingId) is null)
        {
            Logger.Warning($"写入未声明的设置项 \"{settingId}\"", nameof(SettingsService));
        }

        lock (_gate)
        {
            _values[settingId] = JsonSerializer.SerializeToElement(value, SerializerOptions);
            _timer ??= new System.Threading.Timer(Flush, null, Timeout.Infinite, Timeout.Infinite);
            _timer.Change(DebounceMilliseconds, Timeout.Infinite);
        }

        eventAggregator.GetEvent<SettingChangedEvent>().Publish(new SettingChanged(settingId, value));
    }

    /// <summary>
    ///     按 Id 查设置项声明；未命中时重新枚举容器中的全部声明刷新缓存
    ///     （模块在启动序列阶段 2 才注册各自设置项，缓存必须允许后到的声明）
    /// </summary>
    private SettingItemContribution? FindContribution(string settingId)
    {
        if (_declared.TryGetValue(settingId, out var cached))
        {
            return cached;
        }

        foreach (var contribution in containerProvider.Resolve<IEnumerable<SettingItemContribution>>())
        {
            _declared[contribution.Id] = contribution;
        }

        return _declared.GetValueOrDefault(settingId);
    }

    private void Flush(object? state)
    {
        Dictionary<string, JsonElement> snapshot;
        lock (_gate)
        {
            snapshot = new Dictionary<string, JsonElement>(_values, StringComparer.Ordinal);
        }

        // Timer 回调里的异常无人处理会拖垮进程，写入失败必须就地吞掉记日志（同 LayoutPersistence.Flush）
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(snapshot, SerializerOptions));
        }
        catch (Exception exception)
        {
            Logger.Warning($"设置配置写入失败（{exception.GetType().Name}）：{FilePath}", nameof(SettingsService));
        }
    }
}
