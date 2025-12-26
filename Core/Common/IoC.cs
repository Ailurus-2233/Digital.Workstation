namespace DigitalWorkstation.Core.Common;

/// <summary>
/// 依赖注入容器静态访问类
/// <para>提供对 Prism 容器注册和解析的全局访问</para>
/// </summary>
public class IoC
{
    #region Singleton Implementation

    /// <summary>
    /// 单例实例
    /// </summary>
    private static readonly Lazy<IoC> InnerInstance = new(() => new IoC());

    /// <summary>
    /// 获取单例实例
    /// </summary>
    private static IoC Instance => InnerInstance.Value;

    /// <summary>
    /// 私有构造函数，防止外部实例化
    /// </summary>
    private IoC()
    {
    }

    #endregion

    #region 容器注册

    private IContainerRegistry _registry = null!;
    private IContainerProvider _provider = null!;

    /// <summary>
    /// 获取或设置容器注册接口
    /// </summary>
    public static IContainerRegistry Registry
    {
        get => Instance._registry;
        private set => Instance._registry = value;
    }

    /// <summary>
    /// 获取或设置容器解析接口
    /// </summary>
    public static IContainerProvider Provider
    {
        get => Instance._provider;
        private set => Instance._provider = value;
    }

    /// <summary>
    /// 指示 IoC 容器是否已初始化
    /// </summary>
    private bool IsInitialized { get; set; }

    /// <summary>
    /// 初始化 IoC 容器
    /// </summary>
    /// <param name="registry">容器注册接口</param>
    /// <param name="provider">容器解析接口</param>
    /// <exception cref="InvalidOperationException">当 IoC 容器已初始化时抛出此异常</exception>
    public static void Initialize(IContainerRegistry registry, IContainerProvider provider)
    {
        if (Instance.IsInitialized)
        {
            throw new InvalidOperationException("IoC is already initialized");
        }

        Registry = registry;
        Provider = provider;
        Instance.IsInitialized = true;
    }

    #endregion
}