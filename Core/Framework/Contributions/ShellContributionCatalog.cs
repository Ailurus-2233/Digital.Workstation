namespace DigitalWorkstation.Core.Framework.Contributions;

/// <summary>
///     保存贡献登记及其启动批次。只有准备成功的模块对 Shell 可见；普通 DI 服务不参与回滚。
/// </summary>
public sealed class ShellContributionCatalog(IContainerProvider provider)
{
    internal static ContributionBatch BeginBatch(string name) => new(name);

    internal void Prepare(ContributionBatch batch)
    {
        batch.CloseRegistration();
        foreach (var registration in Registrations().Where(item => item.Batch == batch))
        {
            registration.Resolve(provider);
        }
        batch.Commit();
    }

    internal void PrepareUnowned()
    {
        foreach (var registration in Registrations().Where(item => item.Batch is null))
        {
            registration.Resolve(provider);
        }
    }

    public IReadOnlyList<T> Get<T>() where T : class =>
        Registrations()
            .Where(item => item.ContributionType == typeof(T) &&
                           (item.Batch is null || item.Batch.IsVisible))
            .Select(item => (T)item.Resolve(provider))
            .ToArray();

    private IEnumerable<IShellContributionRegistration> Registrations() =>
        provider.Resolve<IEnumerable<IShellContributionRegistration>>();
}

/// <summary>
///     手写贡献与 attribute 扫描共用登记入口；工厂在启动准备时调用一次。
/// </summary>
public static class ShellContributionRegistration
{
    public static void RegisterShellContribution<T>(this IContainerRegistry registry,
        Func<IContainerProvider, T> factory) where T : class
    {
        var batch = ContributionBatch.Current;
        var registration = new ShellContributionRegistration<T>(batch, factory);
        void Register() => registry.RegisterSingleton(typeof(IShellContributionRegistration), _ => registration);
        if (batch is null) Register();
        else batch.Register(Register);
    }

    public static void RegisterShellContribution<T, TImplementation>(this IContainerRegistry registry)
        where T : class where TImplementation : class, T
    {
        registry.RegisterSingleton<TImplementation>();
        registry.RegisterShellContribution<T>(provider => provider.Resolve<TImplementation>());
    }
}

internal interface IShellContributionRegistration
{
    Type ContributionType { get; }
    ContributionBatch? Batch { get; }
    object Resolve(IContainerProvider provider);
}

internal sealed class ShellContributionRegistration<T>(
    ContributionBatch? batch, Func<IContainerProvider, T> factory) : IShellContributionRegistration where T : class
{
    private readonly Lock _gate = new();
    private Lazy<T>? _value;
    public Type ContributionType => typeof(T);
    public ContributionBatch? Batch => batch;

    public object Resolve(IContainerProvider provider)
    {
        Lazy<T> value;
        lock (_gate)
        {
            value = _value ??= new Lazy<T>(() => factory(provider));
        }
        return value.Value;
    }
}

/// <summary>
///     AsyncLocal 传播登记归属；批次关闭后拒绝继承了上下文的迟到后台登记。
///     本批构造器可读取本批声明，其他执行上下文只能读取已提交批次。
/// </summary>
internal sealed class ContributionBatch : IDisposable
{
    private static readonly AsyncLocal<ContributionBatch?> Ambient = new();
    private readonly ContributionBatch? _previous;
    private readonly string _name;
    private readonly Lock _gate = new();
    private bool _registrationOpen = true;
    private bool _committed;
    private bool _rejected;

    internal ContributionBatch(string name)
    {
        _name = name;
        _previous = Ambient.Value;
        Ambient.Value = this;
    }

    internal static ContributionBatch? Current => Ambient.Value;
    internal bool IsVisible
    {
        get { lock (_gate) return !_rejected && (_committed || ReferenceEquals(Current, this)); }
    }
    internal void Register(Action register)
    {
        lock (_gate)
        {
            if (!_registrationOpen)
                throw new InvalidOperationException($"Contribution registration for {_name} is closed.");
            register();
        }
    }
    internal void CloseRegistration()
    {
        lock (_gate) _registrationOpen = false;
    }
    internal void Commit()
    {
        lock (_gate) _committed = true;
    }
    internal void Reject()
    {
        lock (_gate)
        {
            _registrationOpen = false;
            _rejected = true;
        }
    }
    public void Dispose()
    {
        if (!_committed) Reject();
        Ambient.Value = _previous;
    }
}
