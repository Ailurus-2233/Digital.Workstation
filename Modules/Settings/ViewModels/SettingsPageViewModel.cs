using CommunityToolkit.Mvvm.ComponentModel;
using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Framework.Contributions;

namespace DigitalWorkstation.Settings.ViewModels;

/// <summary>
///     设置页 ViewModel：左侧设置分组树（按名称全局合并后的分组），右侧选中分组的设置项编辑器。
///     数据来自 ShellContributionCollector 的设置项收集（ADR-0006 决策 5：普通主视图贡献，无特权机制）
/// </summary>
public sealed partial class SettingsPageViewModel : ObservableObject
{
    private readonly IReadOnlyList<SettingItemContribution> _contributions;
    private readonly ISettingsService _settings;

    public SettingsPageViewModel(ShellContributionCollector collector, ISettingsService settings)
    {
        _settings = settings;
        _contributions = collector.GetSettingItems();
        Groups = collector.GetSettingGroups()
            .Select(group => new SettingGroupModel(group))
            .ToArray();
        // 直接赋字段而不走属性，避免构造期触发一次多余的 Items 重建
        _selectedGroup = Groups.FirstOrDefault();
        RebuildItems();
    }

    /// <summary>
    ///     左侧分组树（组间按位次小者靠前）
    /// </summary>
    public IReadOnlyList<SettingGroupModel> Groups { get; }

    /// <summary>
    ///     当前选中的分组
    /// </summary>
    [ObservableProperty]
    private SettingGroupModel? _selectedGroup;

    /// <summary>
    ///     右侧编辑区：选中分组的设置项（组内按位次小者靠前）
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<SettingItemModel> _items = [];

    partial void OnSelectedGroupChanged(SettingGroupModel? value)
    {
        RebuildItems();
    }

    private void RebuildItems()
    {
        if (SelectedGroup is null)
        {
            Items = [];
            return;
        }

        var items = new List<SettingItemModel>();
        foreach (var contribution in _contributions.Where(
                     contribution => contribution.Group == SelectedGroup.Key))
        {
            var model = CreateItemModel(contribution);
            if (model is not null)
            {
                items.Add(model);
            }
        }

        Items = items;
    }

    /// <summary>
    ///     按声明类型推断编辑器（ADR-0006 决策 8）；当前仅实现 enum → 下拉框，
    ///     其余类型（bool/string/int/double）随首个真实设置项落地时再补，暂记日志跳过
    /// </summary>
    private SettingItemModel? CreateItemModel(SettingItemContribution contribution)
    {
        if (contribution.ValueType.IsEnum)
        {
            return new EnumSettingItemModel(contribution, _settings);
        }

        Logger.Warning(
            $"设置项 \"{contribution.Id}\" 的类型 {contribution.ValueType.Name} 暂无编辑器，已跳过",
            nameof(SettingsPageViewModel));
        return null;
    }
}
