using System.Reflection;
using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.Core.Common;

namespace DigitalWorkstation.Core.Framework.Settings;

/// <summary>
///     attribute 设置注册（ADR-0006 决策 1）：扫描程序集中标注 <see cref="SettingGroupAttribute" /> 的类
///     与标注 <see cref="SettingItemAttribute" /> 的公共静态属性，为每个合法声明生成
///     <see cref="SettingGroupContribution" />/<see cref="SettingItemContribution" /> 元数据注册进容器。
///     扫描只在模块注册时发生一次；不做全局程序集扫描
/// </summary>
public static class SettingRegistration
{
    /// <summary>
    ///     扫描 <paramref name="assembly" /> 中的设置分组与设置项并注册。模块在自身 RegisterTypes 中调用，
    ///     传入本模块程序集。同名分组合并与位次取最小发生在收集侧（跨程序集），此处逐条注册声明
    /// </summary>
    public static void RegisterSettings(this IContainerRegistry registry, Assembly assembly)
    {
        foreach (var type in assembly.DefinedTypes)
        {
            foreach (var group in type.GetCustomAttributes<SettingGroupAttribute>())
            {
                registry.RegisterSingleton(typeof(SettingGroupContribution),
                    _ => new SettingGroupContribution { Name = group.Name, Order = group.Order });
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Static |
                                                        BindingFlags.DeclaredOnly))
            {
                var item = property.GetCustomAttribute<SettingItemAttribute>();
                if (item is null)
                {
                    continue;
                }

                if (property.GetMethod is null)
                {
                    Logger.Warning($"设置项 {type.FullName}.{property.Name} 无 getter，已跳过",
                        nameof(SettingRegistration));
                    continue;
                }

                if (item.DefaultValue is not null && !property.PropertyType.IsInstanceOfType(item.DefaultValue))
                {
                    Logger.Warning(
                        $"设置项 {type.FullName}.{property.Name} 的默认值类型与属性类型 {property.PropertyType.Name} 不匹配，已跳过",
                        nameof(SettingRegistration));
                    continue;
                }

                var metadata = new SettingItemContribution
                {
                    Id = item.Id ?? $"{type.FullName}.{property.Name}",
                    Group = item.Group,
                    Name = item.Name,
                    ValueType = property.PropertyType,
                    DefaultValue = item.DefaultValue,
                    Order = item.Order,
                    RequiresRestart = item.RequiresRestart
                };
                registry.RegisterSingleton(typeof(SettingItemContribution), _ => metadata);
            }
        }
    }
}
