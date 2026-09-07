using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Ursa.Controls;

namespace DigitalWorkstation.Core.Framework.Shell;

/// <summary>
///     带基础布局的窗口基类：内置 VS Code 式五区 shell
///     （ActivityBar/SideBar/MainContent/AuxiliaryPanel/BottomPanel + 状态栏）。
///     PanelAlignment 定义当前窗口的布局：每个枚举值对应一份静态布局模板（FrameworkWindowTheme 的 WindowLayout* 资源），
///     切换即整体替换模板，不做动态调整
/// </summary>
public abstract class FrameworkWindow : UrsaWindow
{
    public static readonly StyledProperty<PanelAlignment> PanelAlignmentProperty =
        AvaloniaProperty.Register<FrameworkWindow, PanelAlignment>(nameof(PanelAlignment), PanelAlignment.Center);

    private readonly FrameworkWindowTheme _theme = new();
    private readonly ContentControl _layoutHost = new();

    protected FrameworkWindow()
    {
        Styles.Add(_theme);
        // 布局模板以窗口 DataContext（ViewModel）为绑定源；Framework 不引用具体 ViewModel 类型，全部宽松绑定
        _layoutHost[!ContentControl.ContentProperty] = this[!DataContextProperty];
        Content = _layoutHost;
        UpdateLayoutTemplate();
    }

    /// <summary>
    ///     继承 UrsaWindow 的窗口主题（标题栏 chrome、模板与焦点行为）
    /// </summary>
    protected override Type StyleKeyOverride => typeof(UrsaWindow);

    /// <summary>
    ///     当前窗口的布局档位；默认居中（= 历史布局）
    /// </summary>
    public PanelAlignment PanelAlignment
    {
        get => GetValue(PanelAlignmentProperty);
        set => SetValue(PanelAlignmentProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == PanelAlignmentProperty)
        {
            UpdateLayoutTemplate();
        }
    }

    private void UpdateLayoutTemplate()
    {
        var key = PanelAlignment switch
        {
            PanelAlignment.Left => "WindowLayoutLeft",
            PanelAlignment.Right => "WindowLayoutRight",
            PanelAlignment.Justify => "WindowLayoutJustify",
            _ => "WindowLayoutCenter"
        };
        // 资源在 StyleInclude 子级（主题构造时已强制加载），经资源查找而不是直接索引 Resources
        _layoutHost.ContentTemplate = _theme.TryGetResource(key, null, out var template)
            ? (IDataTemplate)template!
            : throw new InvalidOperationException($"布局模板资源缺失：{key}");
    }
}
