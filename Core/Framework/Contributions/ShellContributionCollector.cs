using DigitalWorkstation.Core.Abstractions.Commands;
using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.Abstractions.Menus;
using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.Core.Common;

namespace DigitalWorkstation.Core.Framework.Contributions;

/// <summary>
///     贡献收集器：从容器收集模块注册的 shell 贡献，按元数据排序后供 shell 渲染
/// </summary>
public class ShellContributionCollector(IContainerProvider containerProvider)
{
    /// <summary>
    ///     收集全部工具视图贡献（ADR-0002 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)），按 <see cref="ToolViewContribution.Order" /> 升序；
    ///     三处 Bar 与钉住区的分派由消费方按 Placement/AllowMove 决定
    /// </summary>
    public IReadOnlyList<ToolViewContribution> GetToolViews()
    {
        // DryIoc 对零注册的 IEnumerable<具体类> 会按具体类型兜底造一个默认构造的幽灵实例
        // （required 仅编译期约束，Id 为 null）——零工具视图是合法状态，幽灵条目不是贡献，过滤
        return containerProvider.Resolve<IEnumerable<ToolViewContribution>>()
            .Where(view => view.Id is not null)
            .OrderBy(view => view.Order)
            .ToArray();
    }
    /// <summary>
    ///     收集模块贡献的全部 MainContent 主视图
    /// </summary>
    public IReadOnlyList<IMainViewContribution> GetMainViews()
    {
        return containerProvider.Resolve<IEnumerable<IMainViewContribution>>().ToArray();
    }
    /// <summary>
    ///     收集全部菜单贡献（不过滤不排序；分组排序与建树由 <see cref="Menus.MenuTreeBuilder" /> 负责，ADR-0001 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0001-attribute-menu-registration.md)）
    /// </summary>
    public IReadOnlyList<IMenuItemContribution> GetMenuItems()
    {
        return containerProvider.Resolve<IEnumerable<IMenuItemContribution>>().ToArray();
    }
    /// <summary>
    ///     收集全部命令贡献（ADR-0005 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)），按 Order 升序、同 Order 按解析后标题字典序（Ordinal）；
    ///     Id 冲突时保留先注册者，后者丢弃并记日志
    /// </summary>
    public IReadOnlyList<ICommandContribution> GetCommands()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        return containerProvider.Resolve<IEnumerable<ICommandContribution>>()
            .Where(command =>
            {
                if (ids.Add(command.Id))
                {
                    return true;
                }
                Logger.Warning($"Duplicate command ID \"{command.Id}\"; discarding later registration \"{command.Title}\"",
                    nameof(ShellContributionCollector));
                return false;
            })
            .OrderBy(command => command.Order)
            .ThenBy(command => command.Title, StringComparer.Ordinal)
            .ToArray();
    }
    /// <summary>
    ///     收集全部状态栏项，按 <see cref="IStatusBarItemContribution.Order" /> 升序
    /// </summary>
    public IReadOnlyList<IStatusBarItemContribution> GetStatusBarItems()
    {
        return containerProvider.Resolve<IEnumerable<IStatusBarItemContribution>>()
            .OrderBy(item => item.Order)
            .ToArray();
    }
    /// <summary>
    ///     收集全部设置分组：按稳定 Id 全局合并，首个声明的资源来源与名称生效，位次取最小；
    ///     仅被设置项引用而无声明的分组补出，直接显示 Id，ResourceType 为 null，位次为 0。
    ///     按位次升序、同位次按 Id 字典序（Ordinal）；具体贡献类先过滤 DryIoc 幽灵实例
    /// </summary>
    public IReadOnlyList<SettingGroupContribution> GetSettingGroups()
    {
        var groups = containerProvider.Resolve<IEnumerable<SettingGroupContribution>>()
            .Where(group => group.Id is not null)
            .GroupBy(group => group.Id, StringComparer.Ordinal)
            .Select(group =>
            {
                var first = group.First();
                return new SettingGroupContribution
                {
                    Id = group.Key,
                    ResourceType = first.ResourceType,
                    Name = first.Name,
                    Order = group.Min(declaration => declaration.Order)
                };
            })
            .ToList();
        var declaredIds = groups.Select(group => group.Id).ToHashSet(StringComparer.Ordinal);
        groups.AddRange(containerProvider.Resolve<IEnumerable<SettingItemContribution>>()
            .Where(item => item.Id is not null)
            .Select(item => item.Group)
            .Distinct(StringComparer.Ordinal)
            .Where(id => !declaredIds.Contains(id))
            .Select(id => new SettingGroupContribution { Id = id, ResourceType = null, Name = id, Order = 0 }));
        return groups
            .OrderBy(group => group.Order)
            .ThenBy(group => group.Id, StringComparer.Ordinal)
            .ToArray();
    }
    /// <summary>
    ///     收集全部设置项（ADR-0006 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md)），按 Order 升序、同 Order 按名称键字典序（Ordinal）；
    ///     Id 冲突时保留先注册者，后者丢弃并记日志（同 GetCommands）
    /// </summary>
    public IReadOnlyList<SettingItemContribution> GetSettingItems()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        return containerProvider.Resolve<IEnumerable<SettingItemContribution>>()
            .Where(item => item.Id is not null)
            .Where(item =>
            {
                if (ids.Add(item.Id))
                {
                    return true;
                }
                Logger.Warning($"Duplicate setting item ID \"{item.Id}\"; discarding later registration \"{item.Name}\"",
                    nameof(ShellContributionCollector));
                return false;
            })
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .ToArray();
    }
}
