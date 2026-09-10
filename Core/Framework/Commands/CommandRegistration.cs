using System.Reflection;
using DigitalWorkstation.Core.Abstractions.Commands;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Resource;

namespace DigitalWorkstation.Core.Framework.Commands;

/// <summary>
///     attribute 命令注册（ADR-0005）：扫描程序集中标注 <see cref="CommandAttribute" /> 的方法，
///     把宿主类注册为 singleton，并为每个合法方法生成一个 <see cref="ICommandContribution" />
///     实现注册进容器。扫描只在模块注册时发生一次；宿主类实例在 shell 收集时经容器解析一次，之后复用
/// </summary>
public static class CommandRegistration
{
    /// <summary>
    ///     扫描 <paramref name="assembly" /> 中的命令方法并注册。模块在自身 RegisterTypes 中调用，
    ///     传入本模块程序集；不做全局程序集扫描。免类级 attribute：任何类的公共实例方法标注即被扫描
    /// </summary>
    public static void RegisterCommands(this IContainerRegistry registry, Assembly assembly)
    {
        foreach (var type in assembly.DefinedTypes)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(method => (Method: method, Command: method.GetCustomAttribute<CommandAttribute>()))
                .Where(entry => entry.Command is not null);
            var valid = new List<(MethodInfo Method, CommandAttribute Command)>();
            foreach (var (method, command) in methods)
            {
                if (method.GetParameters().Length > 0 ||
                    (method.ReturnType != typeof(void) && method.ReturnType != typeof(Task)))
                {
                    Logger.Warning(
                        $"命令方法 {type.FullName}.{method.Name} 签名非法（仅支持无参 void/Task），已跳过",
                        nameof(CommandRegistration));
                    continue;
                }
                valid.Add((method, command!));
            }

            if (valid.Count == 0)
            {
                continue;
            }

            var hostType = type.AsType();
            registry.RegisterSingleton(hostType);
            foreach (var (method, command) in valid)
            {
                registry.RegisterSingleton(typeof(ICommandContribution),
                    provider => new ReflectedCommandContribution(provider.Resolve(hostType), method, command));
            }
        }
    }
}

/// <summary>
///     由 <see cref="CommandRegistration" /> 生成的命令贡献：包装宿主类实例与方法信息，
///     执行时反射调用；<c>Task</c> 方法异步等待，异常记日志不抛出
/// </summary>
internal sealed class ReflectedCommandContribution : ICommandContribution
{
    private readonly object _instance;
    private readonly MethodInfo _method;

    public ReflectedCommandContribution(object instance, MethodInfo method, CommandAttribute attribute)
    {
        _instance = instance;
        _method = method;
        Id = attribute.Id ?? $"{method.DeclaringType?.FullName}.{method.Name}";
        Title = Language.Get(attribute.Title);
        Gesture = attribute.Gesture;
        Order = attribute.Order;
        Command = new DelegateCommand(Execute);
    }

    public string Id { get; }

    public string Title { get; }

    public string? Gesture { get; }

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
            Logger.Error(actual, $"命令 {_method.DeclaringType?.FullName}.{_method.Name} 执行失败",
                nameof(ReflectedCommandContribution));
        }
    }
}
