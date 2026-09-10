using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using DigitalWorkstation.Core.Abstractions.Commands;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Framework.Layout;
using DigitalWorkstation.Core.Framework.Menus;
using Ursa.Controls;

namespace DigitalWorkstation.Core.Framework.Windows;

/// <summary>
///     带基础布局的窗口基类：内置 VS Code 式五区 shell
///     （ActivityBar/SideBar/MainContent/AuxiliaryPanel/BottomPanel + 状态栏），
///     以及标题栏左侧的菜单栏（全部菜单贡献建树生成，ADR-0001；宽松绑定 ViewModel 的 MenuBarItems）。
///     PanelAlignment 定义当前窗口的布局：每个枚举值对应一份静态布局模板（FrameworkWindowTheme 的 WindowLayout* 资源），
///     切换即整体替换模板，不做动态调整
/// </summary>
public abstract class FrameworkWindow : UrsaWindow
{
    public static readonly StyledProperty<PanelAlignment> PanelAlignmentProperty =
        AvaloniaProperty.Register<FrameworkWindow, PanelAlignment>(nameof(PanelAlignment), PanelAlignment.Center);

    private readonly FrameworkWindowTheme _theme = new();
    private readonly ContentControl _layoutHost = new();
    private readonly CommandPalette _palette = new();

    protected FrameworkWindow()
    {
        Styles.Add(_theme);
        // 布局模板以窗口 DataContext（ViewModel）为绑定源；Framework 不引用具体 ViewModel 类型，全部宽松绑定
        _layoutHost[!ContentControl.ContentProperty] = this[!DataContextProperty];
        // 命令面板（ADR-0005）：顶部浮层代码创建，叠在布局宿主之上（不动四份布局模板）；
        // ItemsSource 宽松绑定 ViewModel 的 Commands 集合，Ctrl+P 直接开关
        _palette[!CommandPalette.ItemsSourceProperty] = new Binding("Commands");
        Content = new Panel { Children = { _layoutHost, _palette } };
        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.P, KeyModifiers.Control),
            Command = new DelegateCommand(_palette.Open)
        });
        // 菜单栏内置于标题栏左侧：Menu 实例与项模板在代码中创建（项模板入窗口 DataTemplates，
        // 子菜单任意深度经模板查找递归复用），chrome-menu 样式在 FrameworkWindowTheme.axaml
        LeftContent = new Menu
        {
            Classes = { "chrome-menu" },
            VerticalAlignment = VerticalAlignment.Center,
            [!ItemsControl.ItemsSourceProperty] = new Binding("MenuBarItems")
        };
        DataTemplates.Add(new FuncDataTemplate<MenuItemViewModel>((item, _) => BuildMenuItemHeader(item!)));
        UpdateLayoutTemplate();
    }

    /// <summary>
    ///     菜单项头部：图标（null 图标不渲染，不留占位间隙）+ 标题；图标前景色由 chrome-menu 样式接管
    /// </summary>
    private static Control BuildMenuItemHeader(MenuItemViewModel item)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        if (item.Icon is { } icon)
        {
            panel.Children.Add(new PathIcon { Data = icon, Width = 14, Height = 14 });
        }
        panel.Children.Add(new TextBlock { Text = item.Title });
        return panel;
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

    /// <summary>
    ///     为带 <see cref="ICommandContribution.Gesture" /> 的命令生成窗口级 KeyBinding（ADR-0005）：
    ///     机制在 Framework、接线在 shell 模块（同 LayoutPersistence 惯例），shell 收集命令后调用一次；
    ///     Gesture 文本无法解析时记日志跳过
    /// </summary>
    public void RegisterCommandGestures(IEnumerable<ICommandContribution> commands)
    {
        foreach (var command in commands)
        {
            if (command.Gesture is not { Length: > 0 } gestureText)
            {
                continue;
            }
            KeyGesture gesture;
            try
            {
                gesture = KeyGesture.Parse(gestureText);
            }
            catch (FormatException exception)
            {
                Logger.Warning(
                    $"命令 \"{command.Title}\" 的快捷键 \"{gestureText}\" 无法解析（{exception.Message}），已跳过",
                    nameof(FrameworkWindow));
                continue;
            }
            KeyBindings.Add(new KeyBinding { Gesture = gesture, Command = command.Command });
        }
    }
}
