using DigitalWorkstation.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DigitalWorkstation.Core.Services.Plugins;
using DigitalWorkstation.Settings.Resources;

namespace DigitalWorkstation.Settings.ViewModels;

public sealed class PluginsPageViewModel
{
    public PluginsPageViewModel(PluginStartupService startup) =>
        Plugins = startup.Plugins.Select(plugin => new PluginStartupItemModel(plugin, startup)).ToArray();

    public IReadOnlyList<PluginStartupItemModel> Plugins { get; }
}
public sealed partial class PluginStartupItemModel : ObservableObject
{
    private readonly PluginStartupService _startup;
    private readonly PluginStartupInfo _plugin;

    public PluginStartupItemModel(PluginStartupInfo plugin, PluginStartupService startup)
    {
        _startup = startup;
        _plugin = plugin;
        _isEnabled = startup.IsEnabled(plugin.Id);
    }

    public string Id => _plugin.Id;
    public string Name => _plugin.Name;

    public string? Error => _plugin.Error;
    public bool HasError => !string.IsNullOrEmpty(Error);
    public string Status => _plugin.IsLoaded ? SettingsResources.PluginLoadedStatus
        : !_plugin.EnabledAtStartup ? SettingsResources.PluginDisabledStatus
        : SettingsResources.PluginFailedStatus;
    public bool IsPendingRestart => _startup.IsPendingRestart(Id);
    public bool IsRemovalPending => _startup.IsRemovalPending(Id);
    public bool CanModify => !IsRemovalPending;

    [RelayCommand(CanExecute = nameof(CanModify))]
    private void Delete()
    {
        _startup.Remove(Id);
        OnPropertyChanged(nameof(IsRemovalPending));
        OnPropertyChanged(nameof(CanModify));
        DeleteCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPendingRestart))]
    private bool _isEnabled;

    partial void OnIsEnabledChanged(bool value) => _startup.SetEnabled(Id, value);
}
