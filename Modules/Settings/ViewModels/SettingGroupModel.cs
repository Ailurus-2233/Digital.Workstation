using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.Core.Resource;

namespace DigitalWorkstation.Settings.ViewModels;

/// <summary>
///     设置页左侧分组树的分组节点：显示名按当前 UI 区域性解析（缺键回退键名本身）
/// </summary>
public sealed class SettingGroupModel(SettingGroupContribution contribution)
{
    /// <summary>
    ///     分组名称键（资源键），设置项按此键归组
    /// </summary>
    public string Key { get; } = contribution.Name;

    /// <summary>
    ///     分组显示名（已解析）
    /// </summary>
    public string Name { get; } = Language.Get(contribution.Name);
}
