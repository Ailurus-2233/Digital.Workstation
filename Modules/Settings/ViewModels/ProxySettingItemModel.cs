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
    private ProxySettings _saved;
    public IReadOnlyList<ProxyModuleChoice> Modules { get; }
    public bool HasModules => Modules.Count > 0;

    public ProxySettingItemModel(SettingItemContribution contribution, ISettingsService settings, NetworkService network)
        : base(contribution, settings)
    {
        _network = network;
        var current = _saved = network.Configuration;
        _enabled = current.Enabled;
        _address = current.Address ?? "";
        _username = current.Username ?? "";
        Modules = network.Modules.OrderBy(module => module.DisplayName)
            .Select(module => new ProxyModuleChoice(module, current.Modules?.Contains(module.Id) == true)).ToArray();
        foreach (var choice in Modules)
            choice.PropertyChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(ModulesModified));
                OnPropertyChanged(nameof(HasDraft));
            };
    }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(EnabledModified), nameof(HasDraft))] private bool _enabled;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(AddressModified), nameof(HasDraft))] private string _address;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(UsernameModified), nameof(HasDraft))] private string _username;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(PasswordModified), nameof(HasDraft))] private string _password = "";
    [ObservableProperty] private string _testTarget = "google.com";
    [ObservableProperty] private string _testStatus = "";

    public bool EnabledModified => Enabled != _saved.Enabled;
    private string TrimmedAddress => Address?.Trim() ?? "";
    private string TrimmedUsername => Username?.Trim() ?? "";
    public bool AddressModified => TrimmedAddress != (_saved.Address ?? "");
    public bool UsernameModified => TrimmedUsername != (_saved.Username ?? "");
    public bool PasswordModified => !string.IsNullOrEmpty(Password);
    public bool ModulesModified =>
        Modules.Any(choice => choice.IsSelected != (_saved.Modules?.Contains(choice.Module.Id) == true));

    public override bool HasDraft =>
        EnabledModified || AddressModified || UsernameModified || PasswordModified || ModulesModified;

    protected override void AcceptDraft()
    {
        _saved = _network.Configuration;
        Password = "";
        OnPropertyChanged(nameof(EnabledModified));
        OnPropertyChanged(nameof(AddressModified));
        OnPropertyChanged(nameof(UsernameModified));
        OnPropertyChanged(nameof(ModulesModified));
    }

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
            var target = TestTarget?.Trim() ?? "";
            var result = await _network.TestProxyAsync(target, TrimmedAddress, TrimmedUsername, Password ?? "", token);
            if (token.IsCancellationRequested) return;
            TestStatus = result switch
            {
                { Status: HttpStatusCode.ProxyAuthenticationRequired } => SettingsResources.ProxyTestAuthFailed,
                { Outcome: ProxyTestOutcome.ResponseReceived, Status: >= HttpStatusCode.OK and < HttpStatusCode.BadRequest } =>
                    string.Format(SettingsResources.ProxyTestSuccess, target, (int)result.Status.Value),
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

    internal override ProxySettings DraftValue
    {
        get
        {
            // Keep choices for temporarily unloaded modules.
            var hidden = (_network.Configuration.Modules ?? []).Except(Modules.Select(choice => choice.Module.Id));
            var selected = hidden.Concat(Modules.Where(choice => choice.IsSelected).Select(choice => choice.Module.Id));
            return new ProxySettings(Enabled, TrimmedAddress, TrimmedUsername, selected.ToArray());
        }
    }
}
