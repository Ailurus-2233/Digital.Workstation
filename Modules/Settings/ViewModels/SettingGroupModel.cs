using CommunityToolkit.Mvvm.ComponentModel;
using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.Core.Resource;

namespace DigitalWorkstation.Settings.ViewModels;

/// <summary>
///     设置页左侧分组树的分组节点：显示名按当前 UI 区域性解析（缺键回退键名本身）
/// </summary>
public sealed class SettingGroupModel : ObservableObject, ISettingGroupDraft
{
    private readonly Func<IReadOnlyDictionary<string, object?>, bool> _save;

    public SettingGroupModel(SettingGroupContribution contribution, IReadOnlyList<SettingItemModel> items,
        Func<IReadOnlyDictionary<string, object?>, bool> save)
    {
        _save = save;
        Key = contribution.Id;
        Name = contribution.ResourceType is { } resourceType
            ? ResourceText.Get(resourceType, contribution.Name)
            : contribution.Name;
        panel = new Lazy<object?>(() => contribution.CreatePanel?.Invoke());
        Items = items;
        foreach (var item in items)
            item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(item.HasDraft)) OnPropertyChanged(nameof(HasDraft));
            };
    }

    private readonly Lazy<object?> panel;
    public object? Panel => panel.Value;

    public T? Get<T>(string settingId) =>
        Items.FirstOrDefault(item => item.Id == settingId)?.DraftValue is T value ? value : default;

    public bool TrySave()
    {
        var drafts = Items.Where(item => item.HasDraft).ToArray();
        if (!_save(drafts.ToDictionary(item => item.Id, item => item.DraftValue))) return false;
        foreach (var item in drafts) item.FinishSave();
        return true;
    }

    public bool HasDraft => Items.Any(item => item.HasDraft);
    public IReadOnlyList<SettingItemModel> Items { get; }

    /// <summary>
    ///     稳定分组 Id，设置项按此标识归组，与显示名资源键无关
    /// </summary>
    public string Key { get; }

    /// <summary>
    ///     分组显示名（已解析）
    /// </summary>
    public string Name { get; }
}

