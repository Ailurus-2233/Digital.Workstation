using DigitalWorkstation.Core.Abstractions.Services;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Services.Contributions;

namespace DigitalWorkstation.Core.Services.Lifecycle;

/// <summary>
/// 保存模块服务的归属、启动顺序及停止责任。只有 Framework 可以启动或撤销一个批次。
/// </summary>
public sealed class ModuleServiceCatalog(IContainerProvider provider) : IDisposable
{
    private readonly Lock _gate = new();
    private readonly List<Registration> _registrations = [];
    private readonly HashSet<Type> _registeredTypes = [];
    private readonly List<StartedService> _started = [];
    private readonly HashSet<ContributionBatch> _startedBatches = [];
    private bool _disposed;

    internal void Register<TService>(IContainerRegistry registry, ContributionBatch batch)
        where TService : class, IModuleService
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var type = typeof(TService);
            if (!_registeredTypes.Add(type))
                throw new InvalidOperationException($"Module service {type.FullName} is already registered.");

            try
            {
                registry.RegisterSingleton<TService>();
                _registrations.Add(new Registration(type, batch));
            }
            catch
            {
                _registeredTypes.Remove(type);
                throw;
            }
        }
    }

    internal void Start(ContributionBatch batch, string ownerKey, bool isPlugin)
    {
        // 与贡献登记共用闸门，先关登记再取快照；不在目录锁内等待批次锁。
        batch.CloseRegistration();
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!_startedBatches.Add(batch))
                throw new InvalidOperationException($"Module services for {batch.Name} have already been started.");

            try
            {
                foreach (var registration in _registrations.Where(item => item.Batch == batch).ToArray())
                {
                    ObjectDisposedException.ThrowIf(_disposed, this);
                    var service = (IModuleService)provider.Resolve(registration.Type);
                    // 先接管停止责任：Start 部分执行后抛异常也必须清理。
                    _started.Add(new StartedService(registration, service, ownerKey, isPlugin));
                    service.Start();
                    ObjectDisposedException.ThrowIf(_disposed, this);
                    Logger.Information(
                        $"{(isPlugin ? "Plugin" : "Module")} service started: {ownerKey}",
                        nameof(ModuleServiceCatalog));
                }
            }
            catch
            {
                batch.Reject();
                Stop(batch);
                throw;
            }
        }
    }

    internal void Stop(ContributionBatch batch)
    {
        lock (_gate)
        {
            // Stop 可能触发退出并重入 Dispose，每次重新寻找仍由本批负责的实例。
            int index;
            while ((index = _started.FindLastIndex(item => item.Registration.Batch == batch)) >= 0)
                StopAt(index);
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            while (_started.Count > 0)
                StopAt(_started.Count - 1);
        }
    }

    // 停止前移除责任，异常或重复退出不会阻止后续服务清理，也不会再次 Stop 同一实例。
    private void StopAt(int index)
    {
        var started = _started[index];
        _started.RemoveAt(index);
        var registration = started.Registration;
        try
        {
            started.Service.Stop();
            Logger.Information(
                $"{(started.IsPlugin ? "Plugin" : "Module")} service stopped: {started.OwnerKey}",
                nameof(ModuleServiceCatalog));
        }
        catch (Exception exception)
        {
            Logger.Error(exception,
                $"Failed to stop {(started.IsPlugin ? "plugin" : "module")} service: {started.OwnerKey}",
                nameof(ModuleServiceCatalog));
        }
    }

    private sealed record Registration(Type Type, ContributionBatch Batch);
    private sealed record StartedService(Registration Registration, IModuleService Service, string OwnerKey, bool IsPlugin);
}