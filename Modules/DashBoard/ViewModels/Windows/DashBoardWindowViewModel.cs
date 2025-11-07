using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DigitalWorkstation.Core.Models.Events;

namespace DigitalWorkstation.DashBoard.ViewModels.Windows;

public partial class DashBoardWindowViewModel (IEventAggregator eventAggregator): ObservableObject
{
    [RelayCommand]
    private void ShowMainWindow()
    {
        eventAggregator.GetEvent<ShowMainWindowEvent>().Publish();
    }
}