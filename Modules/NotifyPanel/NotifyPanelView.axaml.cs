using Avalonia.Controls;
using DigitalWorkstation.Core.Common;

namespace DigitalWorkstation.NotifyPanel;

public partial class NotifyPanelView : UserControl
{
    public NotifyPanelView() : this(IoC.Provider.Resolve<NotifyPanelViewModel>()) { }

    public NotifyPanelView(NotifyPanelViewModel model)
    {
        InitializeComponent();
        DataContext = model;
    }
}
