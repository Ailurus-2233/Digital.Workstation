using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace DigitalWorkstation.Core.Framework.Shell;

/// <summary>
///     FrameworkWindow 的基础布局主题：四份静态布局模板（WindowLayout* 资源）+ 五区 shell 的样式。
///     布局模板整体切换，不做动态调整。
///     经 StyleInclude 从编译进程序集的 axaml 资源加载（与 Semi/Ursa 主题同款机制）；
///     构造时强制 Loaded，保证窗口构造期即可查到布局模板资源
/// </summary>
public class FrameworkWindowTheme : Styles
{
    private static readonly Uri BaseUri = new("avares://DigitalWorkstation.Core.Framework/Shell/");

    public FrameworkWindowTheme()
    {
        var include = new StyleInclude(BaseUri)
        {
            Source = new Uri("FrameworkWindowTheme.axaml", UriKind.Relative)
        };
        _ = include.Loaded;
        Add(include);
    }
}
