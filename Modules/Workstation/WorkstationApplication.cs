using Avalonia.Styling;
using DigitalWorkstation.DashBoard;
using DigitalWorkstation.Framework;

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