namespace DigitalWorkstation.Core.Abstractions.Menus;

/// <summary>
///     声明一个菜单类：类中标注 <see cref="MenuItemAttribute" /> 的公共实例方法成为菜单项，
///     经 MenuRegistration.RegisterMenus 扫描注册（见 ADR-0001 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0001-attribute-menu-registration.md)）。
///     单段路径（如 "shell.file"）时 <see cref="Group" />/<see cref="GroupOrder" /> 描述方法项在该菜单内的分组，
///     <see cref="Order" /> 描述顶层菜单在菜单栏的位次；
///     多段路径（如 "shell.file/export"）时三者描述末端子菜单节点在其父菜单内的分组与位次，
///     方法项进入末端菜单的默认组。
///     路径段为稳定 Id，"/" 分隔，各段 Trim 后按序精确匹配（Ordinal 大小写敏感）。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class MenuGroupAttribute(string path) : Attribute
{
    /// <summary>
    ///     声明末端菜单节点标题；只引用既有路径时使用单参数构造，不依赖菜单所有者的资源
    /// </summary>
    public MenuGroupAttribute(string path, Type resourceType, string titleKey) : this(path)
    {
        ResourceType = resourceType;
        TitleKey = titleKey;
    }

    /// <summary>
    ///     菜单路径："/" 分隔的多级稳定 Id，首段为顶层菜单，不参与本地化
    /// </summary>
    public string Path { get; } = path;

    /// <summary>
    ///     末端菜单节点标题的资源所属类型；仅引用路径时为 null
    /// </summary>
    public Type? ResourceType { get; }

    /// <summary>
    ///     仅为路径末端菜单节点提供标题；仅引用路径时为 null，不参与首个标题声明的选择
    /// </summary>
    public string? TitleKey { get; }

    /// <summary>
    ///     分组名；null = 默认组（GroupOrder 视为 0，排在命名组之前）
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    ///     组的排序权重，小者靠前；同名组多处声明冲突时取最小值
    /// </summary>
    public int GroupOrder { get; set; }

    /// <summary>
    ///     顶层菜单或末端子菜单节点在父级中的排序权重；多处声明取最小值。
    ///     未声明视为"无位次意见"（int.MaxValue，排最后），不参与取最小
    /// </summary>
    public int Order { get; set; } = int.MaxValue;
}
