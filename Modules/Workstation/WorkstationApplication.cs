using Avalonia.Styling;
using DigitalWorkstation.Core.Framework;
using DigitalWorkstation.DashBoard;

namespace DigitalWorkstation.Workstation;

public class WorkstationApplication : FrameworkApplication<MainWindow>
{
    public override void Initialize()
    {
        // Initialization logic here
        RequestedThemeVariant = ThemeVariant.Default;
        base.Initialize();
    }

    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        moduleCatalog.AddModule<DashBoardModule>();
    }
    
}