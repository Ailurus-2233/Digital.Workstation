using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.Resource;
using DigitalWorkstation.Core.UIPackage;

namespace DigitalWorkstation.Workstation.Contributions;

/// <summary>
///     shell 预置的状态栏"就绪"项
/// </summary>
public class ReadyStatusBarItem : IStatusBarItemContribution
{
    public string Id => "shell.status.ready";

    public string Title => Language.StatusReadyTitle;

    public string IconPath => Icons.Ready;

    public int Order => 10;
}
