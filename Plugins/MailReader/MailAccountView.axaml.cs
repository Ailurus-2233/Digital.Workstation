using Avalonia.Controls;
using DigitalWorkstation.Core.Framework.Menus;
using DigitalWorkstation.MailReader.Resources;

namespace DigitalWorkstation.MailReader;

public partial class MailAccountView : UserControl
{
    public MailAccountView() : this(DigitalWorkstation.Core.Common.IoC.Provider.Resolve<MailService>()) { }

    public MailAccountView(MailService service)
    {
        InitializeComponent();
        DataContext = service;
        TabMenu.SetItems(this,
        [
            new(MailResources.Refresh, service.RefreshCommand, "account", 0, 10),
            new(MailResources.ConnectionSettings, service.OpenSettingsCommand, "account", 0, 20)
        ]);
    }
}
