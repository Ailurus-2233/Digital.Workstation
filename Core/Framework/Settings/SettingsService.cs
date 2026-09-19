using System.Text.Json;
using System.Text.Json.Serialization;
using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Framework.Persistence;
using DigitalWorkstation.Core.Models.Events;

namespace DigitalWorkstation.Core.Framework.Settings;

/// <summary>
///     设置服务（ADR-0006 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 3/4）：%AppData%/Digital.Workstation/settings.json 的读/防抖写。
///     启动时经 <see cref="Load" /> 一次性加载入内存；<see cref="Get{T}" /> 纯内存读
///     （未修改时回退声明的默认值，有效声明由 SettingCatalog 与设置页共享）；
///     <see cref="Set{T}" /> 更新内存 + 防抖落盘 + 广播 SettingChangedEvent。
///     读容错仿 LayoutPersistence：文件缺失/损坏一律按无修改处理，只记日志不打断应用。
///     默认值的容器解析假定 UI 线程调用（与 ShellContributionCollector 同约定）
/// </summary>
public sealed class SettingsService(IEventAggregator eventAggregator, SettingCatalog catalog,
    ConfigurationPersistence persistence)
    : ISettingsService
{
    /// <summary>
    ///     设置配置文件路径
    /// </summary>
    public static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Digital.Workstation", "settings.json");

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
    ///     进程启动时的生效值快照（Load 载入内容的副本）：「重启后生效」判定的基准，
    ///     快照不含的项以声明默认值为基准（ADR-0006 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 7）
    /// </summary>
    private readonly Dictionary<string, JsonElement> _sessionStartValues = new(StringComparer.Ordinal);

    /// <summary>
    ///     本次进程内值已偏离启动时生效值的设置项 Id（改回启动值即移除）
    /// </summary>
    private readonly HashSet<string> _pendingRestartIds = new(StringComparer.Ordinal);

    private readonly DebouncedJsonFile<Dictionary<string, JsonElement>> _file =
        persistence.CreateFile<Dictionary<string, JsonElement>>(FilePath, SerializerOptions);

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
                Logger.Warning($"Settings configuration is empty; using default values: {FilePath}",
                    nameof(SettingsService));
                return;
            }

            lock (_gate)
            {
                foreach (var (key, value) in values)
                {
                    _values[key] = value;
                    _sessionStartValues[key] = value;
                }
            }
        }
        catch (Exception exception)
        {
            Logger.Warning($"Failed to read settings configuration ({exception.GetType().Name}); using default values: {FilePath}",
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
                        $"Failed to deserialize persisted value for setting item \"{settingId}\" ({exception.GetType().Name}); using the default value",
                        nameof(SettingsService));
                }
            }
        }

        var contribution = FindContribution(settingId);
        if (contribution is null)
        {
            Logger.Warning($"Reading undeclared setting item \"{settingId}\"; returning the default value",
                nameof(SettingsService));
            return default;
        }

        return contribution.DefaultValue is T value ? value : default;
    }

    public void Set<T>(string settingId, T value)
    {
        var contribution = FindContribution(settingId);
        if (contribution is null)
        {
            Logger.Warning($"Writing undeclared setting item \"{settingId}\"", nameof(SettingsService));
        }

        lock (_gate)
        {
            var element = JsonSerializer.SerializeToElement(value, SerializerOptions);
            _values[settingId] = element;
            _file.ScheduleSave(new Dictionary<string, JsonElement>(_values, StringComparer.Ordinal));
            TrackPendingRestart(settingId, element, contribution);
        }

        eventAggregator.GetEvent<SettingChangedEvent>().Publish(new SettingChanged(settingId, value));
    }

    public bool IsPendingRestart(string settingId)
    {
        lock (_gate)
        {
            return _pendingRestartIds.Contains(settingId);
        }
    }

    /// <summary>
    ///     维护「重启后生效」判定：当前值偏离启动时生效值（快照不含的项以声明默认值为基准）则记入，
    ///     改回启动值则移出。调用方须持有 _gate
    /// </summary>
    private void TrackPendingRestart(string settingId, JsonElement current, SettingItemContribution? contribution)
    {
        if (!_sessionStartValues.TryGetValue(settingId, out var sessionStart) && contribution is not null)
        {
            sessionStart = JsonSerializer.SerializeToElement(contribution.DefaultValue, SerializerOptions);
        }

        if (JsonElement.DeepEquals(sessionStart, current))
        {
            _pendingRestartIds.Remove(settingId);
        }
        else
        {
            _pendingRestartIds.Add(settingId);
        }
    }

    private SettingItemContribution? FindContribution(string settingId) => catalog.Find(settingId);
}
