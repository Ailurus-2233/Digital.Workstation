using System.Text.Json;
using System.Text.Json.Serialization;
using DigitalWorkstation.Core.Common;

namespace DigitalWorkstation.Core.Framework.Layout;

/// <summary>
///     布局持久化服务（ADR-0002）：%AppData%/Digital.Workstation/layout.json 的读/写/删。
///     读容错：文件缺失/损坏/版本不识别 → 返回 null，调用方静默按默认布局启动；
///     写防抖：500ms 内的连续布局变更合并为最后一次落盘。全部失败路径只记日志不打断应用
/// </summary>
public sealed class LayoutPersistence
{
    /// <summary>
    ///     布局配置文件路径
    /// </summary>
    public static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Digital.Workstation", "layout.json");

    private const int DebounceMilliseconds = 500;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly Lock _gate = new();
    private ShellLayoutDto? _pending;
    private System.Threading.Timer? _timer;

    /// <summary>
    ///     读取持久化布局；文件缺失返回 null（首次启动常态），损坏/版本不识别记 Warning 后返回 null
    /// </summary>
    public ShellLayoutDto? Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }

            var layout = JsonSerializer.Deserialize<ShellLayoutDto>(File.ReadAllText(FilePath), SerializerOptions);
            if (layout is null)
            {
                Logger.Warning($"布局配置为空，按默认布局启动：{FilePath}", nameof(LayoutPersistence));
                return null;
            }

            if (layout.Version != ShellLayoutDto.CurrentVersion)
            {
                Logger.Warning($"布局配置版本 {layout.Version} 不识别（当前 {ShellLayoutDto.CurrentVersion}），按默认布局启动：{FilePath}",
                    nameof(LayoutPersistence));
                return null;
            }

            return layout;
        }
        catch (Exception exception)
        {
            Logger.Warning($"布局配置读取失败（{exception.GetType().Name}），按默认布局启动：{FilePath}",
                nameof(LayoutPersistence));
            return null;
        }
    }

    /// <summary>
    ///     调度一次防抖保存：500ms 内的连续调用只落盘最后一份布局
    /// </summary>
    public void ScheduleSave(ShellLayoutDto layout)
    {
        lock (_gate)
        {
            _pending = layout;
            _timer ??= new System.Threading.Timer(Flush, null, Timeout.Infinite, Timeout.Infinite);
            _timer.Change(DebounceMilliseconds, Timeout.Infinite);
        }
    }

    /// <summary>
    ///     删除布局配置文件（重置布局用）；同时作废未落盘的防抖保存，避免文件被重建
    /// </summary>
    public void Delete()
    {
        lock (_gate)
        {
            _pending = null;
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        try
        {
            File.Delete(FilePath);
        }
        catch (Exception exception)
        {
            Logger.Warning($"布局配置删除失败（{exception.GetType().Name}）：{FilePath}", nameof(LayoutPersistence));
        }
    }

    private void Flush(object? state)
    {
        ShellLayoutDto? layout;
        lock (_gate)
        {
            layout = _pending;
            _pending = null;
        }

        if (layout is null)
        {
            return;
        }

        // Timer 回调里的异常无人处理会拖垮进程，写入失败必须就地吞掉记日志
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(layout, SerializerOptions));
        }
        catch (Exception exception)
        {
            Logger.Warning($"布局配置写入失败（{exception.GetType().Name}）：{FilePath}", nameof(LayoutPersistence));
        }
    }
}
