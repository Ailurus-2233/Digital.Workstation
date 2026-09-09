using System.Reflection;
using Avalonia.Controls;
using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Resource;

namespace DigitalWorkstation.Core.Framework.Contributions;

/// <summary>
///     attribute 工具视图注册（ADR-0002）：扫描程序集中标注 <see cref="ToolViewAttribute" /> 的 View 类，
///     为每个合法类生成一个 <see cref="ToolViewContribution" /> 元数据注册进容器，并注册 View 类型本身。
///     扫描只在模块注册时发生一次；不做全局程序集扫描
/// </summary>
public static class ToolViewRegistration
{
    /// <summary>
    ///     扫描 <paramref name="assembly" /> 中的工具视图并注册。模块在自身 RegisterTypes 中调用，
    ///     传入本模块程序集
    /// </summary>
    public static void RegisterToolViews(this IContainerRegistry registry, Assembly assembly)
    {
        var seenIds = new HashSet<string>();
        foreach (var type in assembly.DefinedTypes)
        {
            var attribute = type.GetCustomAttribute<ToolViewAttribute>();
            if (attribute is null)
            {
                continue;
            }

            if (type.IsAbstract || !typeof(Control).IsAssignableFrom(type))
            {
                Logger.Warning($"工具视图 {type.FullName} 必须是可实例化的 Control，已跳过",
                    nameof(ToolViewRegistration));
                continue;
            }

            if (!seenIds.Add(attribute.Id))
            {
                Logger.Warning(
                    $"工具视图 Id \"{attribute.Id}\" 在程序集 {assembly.GetName().Name} 中重复，{type.FullName} 已跳过",
                    nameof(ToolViewRegistration));
                continue;
            }

            var viewType = type.AsType();
            registry.Register(viewType);
            var metadata = new ToolViewContribution
            {
                Id = attribute.Id,
                Title = Language.Get(attribute.TitleKey),
                IconPath = attribute.Icon,
                Order = attribute.Order,
                Placement = attribute.Default,
                AllowMove = attribute.AllowMove,
                ViewType = viewType
            };
            registry.RegisterSingleton(typeof(ToolViewContribution), _ => metadata);
        }
    }
}
