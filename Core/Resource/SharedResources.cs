namespace DigitalWorkstation.Core.Resource;

/// <summary>
///     跨模块共享的产品文案。
/// </summary>
public static class SharedResources
{
    /// <summary>
    ///     产品显示名称
    /// </summary>
    public static string ProductName => ResourceText.Get(typeof(SharedResources), nameof(ProductName));
}
