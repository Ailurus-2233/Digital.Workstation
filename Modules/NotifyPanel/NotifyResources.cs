using Avalonia.Data.Converters;
using DigitalWorkstation.Core.Abstractions.Notifications;
using DigitalWorkstation.Core.Resource;

namespace DigitalWorkstation.NotifyPanel;

public static class NotifyResources
{
    private static string Get(string key) => ResourceText.Get(typeof(NotifyResources), key);

    public static string Title => Get(nameof(Title));
    public static string ClearAll => Get(nameof(ClearAll));
    public static string Empty => Get(nameof(Empty));
    public static string Remove => Get(nameof(Remove));
    public static string Dismiss => Get(nameof(Dismiss));
    public static FuncValueConverter<NotifyLevel, string> LevelLabel { get; } = new(level => Get(level.ToString()));
}
