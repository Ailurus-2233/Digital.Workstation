using System.Text.Json;
using System.Text.Json.Serialization;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Framework.Persistence;

namespace DigitalWorkstation.Core.Framework.Layout;

/// <summary>
///     布局持久化服务（ADR-0002 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）：%AppData%/Digital.Workstation/layout.json 的读/写/删。
///     读容错：文件缺失/损坏/版本不识别 → 返回 null，调用方静默按默认布局启动；
///     写防抖：500ms 内的连续布局变更合并为最后一次落盘。文件读写失败只记日志不打断应用
/// </summary>
public sealed class LayoutPersistence(ConfigurationPersistence persistence)
{
    /// <summary>
    ///     布局配置文件路径
    /// </summary>
    public static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Digital.Workstation", "layout.json");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly DebouncedJsonFile<ShellLayoutDto> _file = persistence.CreateFile<ShellLayoutDto>(FilePath, SerializerOptions);

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
                Logger.Warning($"Layout configuration is empty; using the default layout: {FilePath}",
                    nameof(LayoutPersistence));
                return null;
            }

            if (layout.Version != ShellLayoutDto.CurrentVersion)
            {
                Logger.Warning($"Unrecognized layout configuration version {layout.Version} (current version: {ShellLayoutDto.CurrentVersion}); using the default layout: {FilePath}",
                    nameof(LayoutPersistence));
                return null;
            }

            return layout;
        }
        catch (Exception exception)
        {
            Logger.Warning($"Failed to read layout configuration ({exception.GetType().Name}); using the default layout: {FilePath}",
                nameof(LayoutPersistence));
            return null;
        }
    }

    /// <summary>
    ///     调度一次防抖保存：500ms 内的连续调用只落盘最后一份布局
    /// </summary>
    public void ScheduleSave(ShellLayoutDto layout)
    {
        _file.ScheduleSave(layout);
    }

    /// <summary>
    ///     等待在途写入，作废未落盘的防抖快照，再删除配置文件，避免旧回调重新创建文件。
    /// </summary>
    public void Delete()
    {
        _file.Delete();
    }
}
