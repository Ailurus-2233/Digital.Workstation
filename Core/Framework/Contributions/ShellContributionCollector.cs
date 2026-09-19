using DigitalWorkstation.Core.Abstractions.Commands;
using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.Abstractions.Menus;
using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Framework.Settings;

namespace DigitalWorkstation.Core.Framework.Contributions;

/// <summary>
///     贡献收集器：从容器收集模块注册的 shell 贡献，按元数据排序后供 shell 渲染
/// </summary>
public class ShellContributionCollector(ShellContributionCatalog catalog, SettingCatalog settings)
{
    /// <summary>
    ///     收集全部工具视图贡献（ADR-0002 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)），按 <see cref="ToolViewContribution.Order" /> 升序；
    ///     三处 Bar 与钉住区的分派由消费方按 Placement/AllowMove 决定
    /// </summary>
    public IReadOnlyList<ToolViewContribution> GetToolViews()
    {
        // 只读取显式登记且所属模块准备成功的贡献；空集合不触发具体类兜底。
        return
        [
            .. catalog.Get<ToolViewContribution>()
                .OrderBy(view => view.Order)
        ];
    }

    /// <summary>
    ///     收集模块贡献的全部 MainContent 主视图
    /// </summary>
    public IReadOnlyList<IMainViewContribution> GetMainViews()
    {
        return catalog.Get<IMainViewContribution>().ToArray();
    }

    /// <summary>
    ///     收集全部菜单贡献（不过滤不排序；分组排序与建树由 <see cref="Menus.MenuTreeBuilder" /> 负责，ADR-0001 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0001-attribute-menu-registration.md)）
    /// </summary>
    public IReadOnlyList<IMenuItemContribution> GetMenuItems()
    {
        return catalog.Get<IMenuItemContribution>().ToArray();
    }

    /// <summary>
    ///     收集全部命令贡献（ADR-0005 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)），按 Order 升序、同 Order 按解析后标题字典序（Ordinal）；
    ///     Id 冲突时保留先注册者，后者丢弃并记日志
    /// </summary>
    public IReadOnlyList<ICommandContribution> GetCommands()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        return
        [
            .. catalog.Get<ICommandContribution>()
                .Where(command =>
                {
                    if (ids.Add(command.Id))
                    {
                        return true;
                    }

                    Logger.Warning(
                        $"Duplicate command ID \"{command.Id}\"; discarding later registration \"{command.Title}\"",
                        nameof(ShellContributionCollector));
                    return false;
                })
                .OrderBy(command => command.Order)
                .ThenBy(command => command.Title, StringComparer.Ordinal)
        ];
    }

    /// <summary>
    ///     收集全部状态栏项，按 <see cref="IStatusBarItemContribution.Order" /> 升序
    /// </summary>
    public IReadOnlyList<IStatusBarItemContribution> GetStatusBarItems()
    {
        return catalog.Get<IStatusBarItemContribution>()
            .OrderBy(item => item.Order)
            .ToArray();
    }

    /// <summary>
    ///     设置页与设置读取服务共享同一有效声明目录。
    /// </summary>
    public IReadOnlyList<SettingGroupContribution> GetSettingGroups() => settings.GetGroups();

    public IReadOnlyList<SettingItemContribution> GetSettingItems() => settings.GetItems();
}
