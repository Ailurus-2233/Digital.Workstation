using Avalonia.Controls;

namespace DigitalWorkstation.MailReader;

public partial class MailReadingView : UserControl
{
    public MailReadingView() : this(DigitalWorkstation.Core.Common.IoC.Provider.Resolve<MailService>()) { }

    public MailReadingView(MailService service)
    {
        InitializeComponent();
        DataContext = service;
    }
}
