namespace DigitalWorkstation.Core.Abstractions.Settings;

public interface ISettingValues
{
    T? Get<T>(string settingId);
}

/// <summary>The current editor values for a settings panel and a way to commit just its group.</summary>
public interface ISettingGroupDraft : ISettingValues
{
    bool TrySave();
}
