using System.Reflection;
using DigitalWorkstation.Core.Abstractions.Menus;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Resource;

namespace DigitalWorkstation.Core.Framework.Menus;

/// <summary>
///     attribute 菜单注册：扫描程序集中标注 <see cref="MenuGroupAttribute" /> 的类，
///     把菜单类注册为 singleton，并为每个合法的 <see cref="MenuItemAttribute" /> 方法
///     生成一个 <see cref="IMenuItemContribution" /> 实现注册进容器（ADR-0001）。
///     扫描只在模块注册时发生一次；菜单类实例在 shell 建树时经容器解析一次，之后复用
/// </summary>
public static class MenuRegistration
{
    /// <summary>
    ///     扫描 <paramref name="assembly" /> 中的菜单类并注册。模块在自身 RegisterTypes 中调用，
    ///     传入本模块程序集；不做全局程序集扫描
    /// </summary>
    public static void RegisterMenus(this IContainerRegistry registry, Assembly assembly)
    {
        foreach (var type in assembly.DefinedTypes)
        {
            var group = type.GetCustomAttribute<MenuGroupAttribute>();
            if (group is null)
            {
                continue;
            }

            if (group.Path.Split('/', StringSplitOptions.TrimEntries).Any(segment => segment.Length == 0))
            {
                Logger.Warning($"菜单类 {type.FullName} 的路径 \"{group.Path}\" 含空段，整类跳过",
                    nameof(MenuRegistration));
                continue;
            }

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(method => (Method: method, Item: method.GetCustomAttribute<MenuItemAttribute>()))
                .Where(entry => entry.Item is not null)
                .ToArray();
            var valid = new List<(MethodInfo Method, MenuItemAttribute Item)>();
            foreach (var (method, item) in methods)
            {
                if (method.GetParameters().Length > 0 ||
                    (method.ReturnType != typeof(void) && method.ReturnType != typeof(Task)))
                {
                    Logger.Warning(
                        $"菜单方法 {type.FullName}.{method.Name} 签名非法（仅支持无参 void/Task），已跳过",
                        nameof(MenuRegistration));
                    continue;
                }
                valid.Add((method, item!));
            }

            if (valid.Count == 0)
            {
                continue;
            }

            var menuType = type.AsType();
            registry.RegisterSingleton(menuType);
            foreach (var (method, item) in valid)
            {
                registry.RegisterSingleton(typeof(IMenuItemContribution),
                    provider => new ReflectedMenuItemContribution(provider.Resolve(menuType), method, item, group));
            }
        }
    }
}

/// <summary>
///     由 <see cref="MenuRegistration" /> 生成的菜单项贡献：包装菜单类实例与方法信息，
///     点击时反射调用；<c>Task</c> 方法异步等待，异常记日志不抛出
/// </summary>
internal sealed class ReflectedMenuItemContribution : IMenuItemContribution
{
    private readonly object _instance;
    private readonly MethodInfo _method;

    public ReflectedMenuItemContribution(object instance, MethodInfo method, MenuItemAttribute item,
        MenuGroupAttribute group)
    {
        _instance = instance;
        _method = method;
        Title = Language.Get(item.Title);
        IconPath = item.Icon;
        Path = group.Path;
        Group = group.Group;
        GroupOrder = group.GroupOrder;
        NodeOrder = group.Order;
        Order = item.Order;
        Command = new DelegateCommand(Execute);
    }

    public string Title { get; }

    public string? IconPath { get; }

    public string Path { get; }

    public string? Group { get; }

    public int GroupOrder { get; }

    public int NodeOrder { get; }

    public int Order { get; }

    public System.Windows.Input.ICommand Command { get; }

    private void Execute()
    {
        _ = ExecuteAsync();
    }

    private async Task ExecuteAsync()
    {
        try
        {
            if (_method.Invoke(_instance, null) is Task task)
            {
                await task;
            }
        }
        catch (Exception exception)
        {
            var actual = exception is TargetInvocationException { InnerException: not null } invocation
                ? invocation.InnerException
                : exception;
            Logger.Error(actual, $"菜单命令 {_method.DeclaringType?.FullName}.{_method.Name} 执行失败",
                nameof(ReflectedMenuItemContribution));
        }
    }
}
