using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DigitalWorkstation.Core.Services.Plugins;
using DigitalWorkstation.Settings.Resources;

namespace DigitalWorkstation.Settings.ViewModels;

public sealed class PluginsPageViewModel : ObservableObject
{
    private readonly PluginStartupService _startup;

    public PluginsPageViewModel(PluginStartupService startup)
    {
        _startup = startup;
        Plugins = startup.Plugins.Select(plugin => new PluginStartupItemModel(plugin, startup)).ToArray();
        foreach (var plugin in Plugins)
            plugin.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(plugin.HasDraft)) OnPropertyChanged(nameof(HasDraft));
            };
    }

    public IReadOnlyList<PluginStartupItemModel> Plugins { get; }
    public bool HasDraft => Plugins.Any(plugin => plugin.HasDraft);

    public bool Save()
    {
        var drafts = Plugins.Where(plugin => plugin.HasDraft).ToArray();
        if (!_startup.TrySaveStartupChoices(drafts.ToDictionary(plugin => plugin.Id, plugin => plugin.IsEnabled)))
            return false;
        foreach (var plugin in drafts) plugin.FinishSave();
        return true;
    }
}
public sealed partial class PluginStartupItemModel : DraftOptionModel
{
    private readonly PluginStartupService _startup;
    private readonly PluginStartupInfo _plugin;
    private bool _savedEnabled;

    public PluginStartupItemModel(PluginStartupInfo plugin, PluginStartupService startup)
    {
        _startup = startup;
        _plugin = plugin;
        _savedEnabled = _isEnabled = startup.IsEnabled(plugin.Id);
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
        IsEnabled = _startup.IsEnabled(Id);
        FinishSave();
        OnPropertyChanged(nameof(IsRemovalPending));
        OnPropertyChanged(nameof(CanModify));
        DeleteCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasDraft))]
    private bool _isEnabled;

    public override bool HasDraft => CanModify && IsEnabled != _savedEnabled;
    protected override void AcceptDraft()
    {
        _savedEnabled = IsEnabled;
        OnPropertyChanged(nameof(IsPendingRestart));
    }
}
