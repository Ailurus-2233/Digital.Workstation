using DigitalWorkstation.Core.Services;
using DigitalWorkstation.Core.Services.Network;
using DigitalWorkstation.Core.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Framework.Contributions;
using DigitalWorkstation.Core.Common.Events;
using DigitalWorkstation.Settings.Resources;

namespace DigitalWorkstation.Settings.ViewModels;

/// <summary>
///     设置页 ViewModel：左侧设置分组树（按稳定 Id 合并后的分组），右侧选中分组的设置项编辑器。
///     数据来自 ShellContributionCollector 的设置项收集（普通主视图贡献，无特权机制）。
///     另承载需重启 UX：订阅 SettingChangedEvent 刷新项级「重启后生效」标记与顶部横幅
/// </summary>

public sealed partial class SettingsPageViewModel : ObservableObject, IEditableTab
{
    private readonly IReadOnlyList<SettingItemContribution> _contributions;
    private readonly SettingsService _settings;
    private readonly NetworkService _network;

    public SettingsPageViewModel(ShellContributionCollector collector, SettingsService settings,
        IEventAggregator eventAggregator, NetworkService network, PluginsPageViewModel pluginsPage)
    {
        _settings = settings;
        _network = network;
        PluginsPage = pluginsPage;
        _contributions = collector.GetSettingItems();
        var itemsByGroup = _contributions.ToLookup(item => item.Group);
        Groups = collector.GetSettingGroups()
            .Select(group => new SettingGroupModel(group,
                itemsByGroup[group.Id].Select(CreateItemModel).OfType<SettingItemModel>().ToArray(), settings.TrySave))
            .ToArray();
        foreach (var group in Groups)
            group.PropertyChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(HasSettingsDraft));
                NotifyDraftChanged();
                RestartNowCommand.NotifyCanExecuteChanged();
            };
        PluginsPage.PropertyChanged += (_, _) =>
        {
            NotifyDraftChanged();
            RestartNowCommand.NotifyCanExecuteChanged();
        };
        _selectedGroup = Groups.FirstOrDefault();
        _hasPendingRestartChanges = ComputeHasPendingRestartChanges();
        eventAggregator.GetEvent<SettingChangedEvent>().Subscribe(OnSettingChanged);
    }

    /// <summary>
    ///     左侧分组树（组间按位次小者靠前）
    /// </summary>
    public IReadOnlyList<SettingGroupModel> Groups { get; }
    public bool HasSettingsDraft => Groups.Any(group => group.HasDraft);
    public bool HasUnsavedChanges => HasSettingsDraft || PluginsPage.HasDraft;
    public string ClosePrompt => SettingsResources.UnsavedSettingsPrompt;
    public long DraftVersion { get; private set; }
    public Task? PendingSave => SaveCommand.ExecutionTask is { IsCompleted: false } task ? task : null;

    private void NotifyDraftChanged()
    {
        DraftVersion++;
        OnPropertyChanged(nameof(HasUnsavedChanges));
    }

    public async Task<bool> SaveChangesAsync()
    {
        await (PendingSave ?? SaveCommand.ExecuteAsync(null));
        return !HasUnsavedChanges;
    }

    /// <summary>
    ///     当前选中的分组
    /// </summary>
    [ObservableProperty]
    private SettingGroupModel? _selectedGroup;

    public PluginsPageViewModel PluginsPage { get; }

    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private string _saveStatus = "";

    partial void OnSelectedGroupChanged(SettingGroupModel? value) => SaveStatus = "";
    partial void OnSelectedTabIndexChanged(int value) => SaveStatus = "";

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            await SaveDraftsAsync();
        }
        catch (Exception error)
        {
            Logger.Error(error, "Failed to save settings.", nameof(SettingsPageViewModel));
            SaveStatus = SettingsResources.SettingsSaveFailed;
        }
    }

    private async Task SaveDraftsAsync()
    {
        SaveStatus = "";
        var drafts = Groups.SelectMany(group => group.Items).Where(item => item.HasDraft).ToArray();
        var changes = drafts.ToDictionary(item => item.Id, item => item.DraftValue);
        var proxy = drafts.OfType<ProxySettingItemModel>().SingleOrDefault();
        bool settingsSaved;
        if (proxy is null)
        {
            settingsSaved = _settings.TrySave(changes);
        }
        else
        {
            var result = await _network.SaveAsync((ProxySettings)changes[proxy.Id]!, proxy.Password ?? "", updated =>
            {
                changes[proxy.Id] = updated;
                return _settings.TrySave(changes);
            });
            if (result is not (ProxySaveResult.Saved or ProxySaveResult.ConfigurationWriteFailed))
            {
                SelectedTabIndex = 0;
                SelectedGroup = Groups.First(group => group.Items.Contains(proxy));
                SaveStatus = result switch
                {
                    ProxySaveResult.InvalidAddress => SettingsResources.ProxyInvalidAddress,
                    ProxySaveResult.UsernameRequired => SettingsResources.ProxyUsernameRequired,
                    ProxySaveResult.PasswordRequired => SettingsResources.ProxyPasswordRequired,
                    _ => SettingsResources.ProxyStorageUnavailable
                };
                return;
            }
            settingsSaved = result == ProxySaveResult.Saved;
        }

        if (settingsSaved)
            foreach (var item in drafts) item.FinishSave();
        var hasPluginDraft = PluginsPage.HasDraft;
        var pluginsSaved = !hasPluginDraft || PluginsPage.Save();
        if (!settingsSaved || !pluginsSaved)
        {
            SaveStatus = SettingsResources.SettingsSaveFailed;
            return;
        }
        SaveStatus = !hasPluginDraft && drafts is [ProxySettingItemModel]
            ? SettingsResources.ProxySaved
            : SettingsResources.SettingsSaved;
    }

    /// <summary>
    ///     顶部横幅是否可见：存在未生效的需重启修改
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RestartNowCommand))]
    private bool _hasPendingRestartChanges;

    /// <summary>
    ///     横幅文本
    /// </summary>
    public string RestartBannerText => SettingsResources.SettingsRestartBannerText;

    /// <summary>
    ///     横幅「立即重启」按钮标题
    /// </summary>
    public string RestartNowButtonTitle => SettingsResources.SettingsRestartNowButtonTitle;

    /// <summary>
    ///     横幅「立即重启」：强制落盘后启动新进程并退出当前进程
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRestartNow))]
    private void RestartNow()
    {
        if (CanRestartNow()) ApplicationRestarter.Restart();
    }

    private bool CanRestartNow() => HasPendingRestartChanges && !HasSettingsDraft && !PluginsPage.HasDraft;

    /// <summary>
    ///     设置变更到达：刷新对应项的「重启后生效」标记与横幅可见性（改回启动值时两者随之消失）
    /// </summary>
    private void OnSettingChanged(SettingChanged changed)
    {
        foreach (var item in Groups.SelectMany(group => group.Items))
        {
            if (item.Id == changed.SettingId)
            {
                item.RefreshRestartPending();
            }
        }

        HasPendingRestartChanges = ComputeHasPendingRestartChanges();
    }

    private bool ComputeHasPendingRestartChanges()
    {
        return _contributions.Any(
            contribution => contribution.RequiresRestart && _settings.IsPendingRestart(contribution.Id));
    }

    /// <summary>
    ///     按声明类型推断编辑器；支持 string → 文本框和 enum → 下拉框，
    ///     其余类型（bool/int/double）随首个真实设置项落地时再补，暂记日志跳过
    /// </summary>
    private SettingItemModel? CreateItemModel(SettingItemContribution contribution)
    {
        if (contribution.Id == ProxySettings.SettingId) return new ProxySettingItemModel(contribution, _settings, _network);
        if (contribution.ValueType == typeof(string)) return new StringSettingItemModel(contribution, _settings);
        if (contribution.ValueType.IsEnum)
        {
            return new EnumSettingItemModel(contribution, _settings);
        }

        Logger.Warning(
            $"No editor is available for setting item \"{contribution.Id}\" of type {contribution.ValueType.Name}; skipping",
            nameof(SettingsPageViewModel));
        return null;
    }
}
