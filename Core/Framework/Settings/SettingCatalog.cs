using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Framework.Contributions;

namespace DigitalWorkstation.Core.Framework.Settings;

/// <summary>
///     设置声明的唯一解释入口：重复 Id 首个有效声明生效，分组只补齐有效设置项的引用。
///     每次读取当前可用贡献，不缓存尚未提交的模块声明，也不让无关查询改变已选声明。
/// </summary>
public sealed class SettingCatalog(ShellContributionCatalog contributions)
{
    private readonly Lock _gate = new();
    private readonly HashSet<SettingItemContribution> _reportedDuplicates = [];

    public SettingItemContribution? Find(string id) =>
        GetItems().FirstOrDefault(item => StringComparer.Ordinal.Equals(item.Id, id));

    public IReadOnlyList<SettingItemContribution> GetItems()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var items = new List<SettingItemContribution>();
        foreach (var item in contributions.Get<SettingItemContribution>())
        {
            if (ids.Add(item.Id))
            {
                items.Add(item);
                continue;
            }
            lock (_gate)
            {
                if (_reportedDuplicates.Add(item))
                    Logger.Warning($"Duplicate setting item ID {item.Id}; discarding later registration {item.Name}",
                        nameof(SettingCatalog));
            }
        }
        return items.OrderBy(item => item.Order).ThenBy(item => item.Name, StringComparer.Ordinal).ToArray();
    }

    public IReadOnlyList<SettingGroupContribution> GetGroups()
    {
        var groups = contributions.Get<SettingGroupContribution>()
            .GroupBy(group => group.Id, StringComparer.Ordinal)
            .Select(group =>
            {
                var first = group.First();
                return new SettingGroupContribution
                {
                    Id = group.Key, ResourceType = first.ResourceType, Name = first.Name,
                    Order = group.Min(declaration => declaration.Order)
                };
            }).ToList();
        var declared = groups.Select(group => group.Id).ToHashSet(StringComparer.Ordinal);
        groups.AddRange(GetItems().Select(item => item.Group).Distinct(StringComparer.Ordinal)
            .Where(id => !declared.Contains(id))
            .Select(id => new SettingGroupContribution { Id = id, ResourceType = null, Name = id, Order = 0 }));
        return groups.OrderBy(group => group.Order).ThenBy(group => group.Id, StringComparer.Ordinal).ToArray();
    }
}
