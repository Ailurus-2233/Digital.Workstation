using DigitalWorkstation.Core.Services.Lifecycle;
using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.Core.Abstractions.WindowManager;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Services.Contributions;
using DigitalWorkstation.Core.Services.Layout;
using DigitalWorkstation.Core.Services.Persistence;
using DigitalWorkstation.Core.Services.Plugins;
using DigitalWorkstation.Core.Services.Settings;
using DigitalWorkstation.Core.Services.WindowManager;

namespace DigitalWorkstation.Core.Services;

/// <summary>
/// 基础服务注册与启动入口。Framework 决定启动时机及退出时机，服务实现不引用 Framework。
/// </summary>
public sealed class BasicServices(
    SettingsService settings,
    PluginStartupService plugins,
    ConfigurationPersistence persistence,
    LayoutPersistence layout) : IDisposable
{
    private bool _started;
    private bool _disposed;

    public static void Register(IContainerRegistry registry)
    {
        var windows = new FrameworkWindowManager();
        registry.RegisterSingleton<IMainWindowManager>(() => windows);
        registry.RegisterSingleton<IWindowManager>(() => windows);
        registry.RegisterSingleton<ShellContributionCatalog>();
        registry.RegisterSingleton<SettingCatalog>();
        registry.RegisterSingleton<ConfigurationPersistence>();
        registry.RegisterSingleton<LayoutPersistence>();
        registry.RegisterSingleton<PluginStartupService>();
        registry.RegisterSingleton<SettingsService>();
        registry.RegisterSingleton(typeof(ISettingsService), provider => provider.Resolve<SettingsService>());
        registry.RegisterSingleton<ModuleServiceCatalog>();
        registry.RegisterSingleton<BasicServices>();
    }

    /// <summary>框架设置声明登记后、应用语言和创建 Shell 前调用。</summary>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_started) return;
        settings.Load();
        plugins.Load();
        // 提前准备布局写入器，统一纳入配置保存生命周期。
        _ = layout;
        _started = true;
        Logger.Information("Basic services started", nameof(BasicServices));
    }

    /// <summary>实际退出时统一保存配置并释放计时器；可取消的关窗不调用。</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        persistence.Dispose();
        Logger.Information("Basic services stopped", nameof(BasicServices));
    }
}