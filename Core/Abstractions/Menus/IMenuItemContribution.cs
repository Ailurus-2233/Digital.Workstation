using System.Windows.Input;

namespace DigitalWorkstation.Core.Abstractions.Menus;

/// <summary>
///     模块向菜单栏贡献菜单项的契约（路径/分组模型，见 ADR-0001）。
///     通常不直接实现本接口：模块用 <see cref="MenuGroupAttribute" />/<see cref="MenuItemAttribute" />
///     标注普通类，经 MenuRegistration.RegisterMenus 扫描后生成本契约的实现注册进容器；
///     shell 收集全部实现后建树（分组排序、组间分隔线）并渲染。
/// </summary>
public interface IMenuItemContribution
{
    /// <summary>
    ///     显示标题（已按当前 UI 区域性解析）
    /// </summary>
    string Title { get; }

    /// <summary>
    ///     图标的 StreamGeometry path 字符串，由 PathIcon 消费并随主题变色；null = 无图标
    /// </summary>
    string? IconPath { get; }

    /// <summary>
    ///     完整菜单路径："/" 分隔，段为 Language 资源键，首段为顶层菜单
    /// </summary>
    string Path { get; }

    /// <summary>
    ///     单段路径：本条目在该菜单内的组；多段路径：末端子菜单节点在其父菜单内的组。null = 默认组
    /// </summary>
    string? Group { get; }

    /// <summary>
    ///     组的排序权重，小者靠前；同名组多处声明冲突时取最小值
    /// </summary>
    int GroupOrder { get; }

    /// <summary>
    ///     顶层菜单（单段路径）或末端子菜单节点（多段路径）在父级中的排序权重；多处声明取最小值
    /// </summary>
    int NodeOrder { get; }

    /// <summary>
    ///     条目在组内的排序权重，小者靠前；同 Order 按解析后的 <see cref="Title" /> 字典序
    /// </summary>
    int Order { get; }

    /// <summary>
    ///     点击菜单项执行的命令
    /// </summary>
    ICommand Command { get; }
}
