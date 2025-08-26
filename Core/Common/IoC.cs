namespace DigitalWorkstation.Common;

public class IoC {
    #region Singleton Implementation

    private static readonly Lazy<IoC> InnerInstance = new(() => new IoC());

    private static IoC Instance => InnerInstance.Value;

    private IoC() { }

    #endregion

    #region 容器注册

    private IContainerRegistry _registry = null!;
    private IContainerProvider _provider = null!;

    public static IContainerRegistry Registry {
        get => Instance._registry;
        private set => Instance._registry = value;
    }

    public static IContainerProvider Provider {
        get => Instance._provider;
        private set => Instance._provider = value;
    }


    private bool IsInitialized { get; set; }


    public static void Initialize(IContainerRegistry registry, IContainerProvider provider) {
        if (Instance.IsInitialized) {
            throw new InvalidOperationException("IoC is already initialized");
        }

        Registry = registry;
        Provider = provider;
        Instance.IsInitialized = true;
    }

    #endregion
}