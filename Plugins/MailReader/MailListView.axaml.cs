using Avalonia.Controls;
using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.MailReader.Resources;

namespace DigitalWorkstation.MailReader;

[ToolView("mail-reader.inbox", typeof(MailResources), nameof(MailResources.PluginName),
    Default = ToolViewPlacement.ActivityBar, Order = 10,
    Icon = "M2 4h20v16H2V4m2 2v1l8 5 8-5V6l-8 5-8-5m0 3v9h16V9l-8 5-8-5")]
public partial class MailListView : UserControl
{
    public MailListView() : this(DigitalWorkstation.Core.Common.IoC.Provider.Resolve<MailService>()) { }

    public MailListView(MailService service)
    {
        InitializeComponent();
        DataContext = service;
    }
}
