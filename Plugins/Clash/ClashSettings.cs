using DigitalWorkstation.Core.Abstractions.Settings;

namespace DigitalWorkstation.Clash;

[SettingGroup("clash", typeof(ClashResources), "Title", PanelType = typeof(CoreSettingsPanel))]
public static class ClashSettings
{
    public const string PortId = "clash.port";

    [SettingItem("clash", typeof(ClashResources), "Port", Id = PortId, DefaultValue = "7897", Order = 0)]
    public static string Port => "7897";
}
