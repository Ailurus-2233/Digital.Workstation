using DigitalWorkstation.Core.Framework;
using DigitalWorkstation.DashBoard;

namespace DigitalWorkstation.Workstation;

public class WorkstationApplication : FrameworkApplication<MainWindow>
{
    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        moduleCatalog.AddModule<DashBoardModule>();
    }
    
}