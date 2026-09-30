using DigitalWorkstation.Core.Abstractions.Services;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Services.Contributions;

namespace DigitalWorkstation.Core.Services.Lifecycle;

/// <summary>在模块的 RegisterTypes 中登记服务单例，构造和启动交给生命周期目录。</summary>
public static class ModuleServiceRegistration
{
    public static void RegisterModuleService<TService>(this IContainerRegistry registry)
        where TService : class, IModuleService
    {
        var batch = ContributionBatch.Current
            ?? throw new InvalidOperationException("Module services must be registered during module initialization.");
        batch.Register(() => IoC.Provider.Resolve<ModuleServiceCatalog>().Register<TService>(registry, batch));
    }
}