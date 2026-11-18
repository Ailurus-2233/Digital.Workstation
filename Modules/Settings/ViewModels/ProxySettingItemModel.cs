using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Net;
using DigitalWorkstation.Core.Abstractions.Network;
using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.Core.Services.Network;
using DigitalWorkstation.Settings.Resources;

namespace DigitalWorkstation.Settings.ViewModels;

public sealed partial class ProxyModuleChoice(ProxyModule module, bool selected) : ObservableObject
{
    public ProxyModule Module { get; } = module;
    [ObservableProperty] private bool _isSelected = selected;
}

public sealed partial class ProxySettingItemModel : SettingItemModel
{
    private readonly NetworkService _network;
    public IReadOnlyList<ProxyModuleChoice> Modules { get; }

    public ProxySettingItemModel(SettingItemContribution contribution, ISettingsService settings, NetworkService network)
        : base(contribution, settings)
    {
        _network = network;
        var current = network.Configuration;
        _enabled = current.Enabled;
        _address = current.Address ?? "";
        _username = current.Username ?? "";
        Modules = network.Modules.OrderBy(module => module.DisplayName)
            .Select(module => new ProxyModuleChoice(module, current.Modules?.Contains(module.Id) == true)).ToArray();
    }

    [ObservableProperty] private bool _enabled;
    [ObservableProperty] private string _address;
    [ObservableProperty] private string _username;
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _status = "";
    [ObservableProperty] private string _testTarget = "google.com";
    [ObservableProperty] private string _testStatus = "";

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(TestTarget) or nameof(Address) or nameof(Username) or nameof(Password))
        {
            TestConnectionCommand.Cancel();
            TestStatus = "";
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync(CancellationToken token)
    {
        TestStatus = SettingsResources.ProxyTesting;
        try
        {
            var result = await _network.TestProxyAsync(TestTarget, Address, Username, Password, token);
            if (token.IsCancellationRequested) return;
            TestStatus = result switch
            {
                { Status: HttpStatusCode.ProxyAuthenticationRequired } => SettingsResources.ProxyTestAuthFailed,
                { Outcome: ProxyTestOutcome.ResponseReceived, Status: >= HttpStatusCode.OK and < HttpStatusCode.BadRequest } =>
                    string.Format(SettingsResources.ProxyTestSuccess, TestTarget.Trim(), (int)result.Status.Value),
                { Status: { } code } => string.Format(SettingsResources.ProxyTestHttpError, (int)code),
                { Outcome: ProxyTestOutcome.InvalidTarget } => SettingsResources.ProxyTestInvalidTarget,
                { Outcome: ProxyTestOutcome.InvalidProxy } => SettingsResources.ProxyInvalidAddress,
                { Outcome: ProxyTestOutcome.UsernameRequired } => SettingsResources.ProxyUsernameRequired,
                { Outcome: ProxyTestOutcome.CredentialsUnavailable } => SettingsResources.ProxyPasswordRequired,
                { Outcome: ProxyTestOutcome.TimedOut } => SettingsResources.ProxyTestTimeout,
                _ => SettingsResources.ProxyTestFailed
            };
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        // Keep the choices of temporarily unloaded plugins when editing the visible list.
        var hidden = (_network.Configuration.Modules ?? []).Except(Modules.Select(choice => choice.Module.Id));
        var selected = hidden.Concat(Modules.Where(choice => choice.IsSelected).Select(choice => choice.Module.Id));
        var config = new ProxySettings(Enabled, Address.Trim(), Username.Trim(), selected.ToArray());
        var result = await _network.SaveAsync(config, Password);
        Status = result switch
        {
            ProxySaveResult.Saved => SettingsResources.ProxySaved,
            ProxySaveResult.InvalidAddress => SettingsResources.ProxyInvalidAddress,
            ProxySaveResult.UsernameRequired => SettingsResources.ProxyUsernameRequired,
            ProxySaveResult.PasswordRequired => SettingsResources.ProxyPasswordRequired,
            _ => SettingsResources.ProxyStorageUnavailable
        };
        if (result == ProxySaveResult.Saved) Password = "";
    }
}
