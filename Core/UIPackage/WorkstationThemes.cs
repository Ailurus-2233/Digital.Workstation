using Avalonia.Styling;
using Semi.Avalonia;
using Semi.Avalonia.ColorPicker;
using Semi.Avalonia.DataGrid;
using Semi.Avalonia.Dock;

namespace DigitalWorkstation.Core.UIPackage;

public class WorkstationTheme : Styles
{
    public WorkstationTheme()
    {
        Add(new SemiTheme());
        Add(new Ursa.Themes.Semi.SemiTheme());
        Add(new ColorPickerSemiTheme());
        Add(new DataGridSemiTheme());
        Add(new DockSemiTheme());
    }
}