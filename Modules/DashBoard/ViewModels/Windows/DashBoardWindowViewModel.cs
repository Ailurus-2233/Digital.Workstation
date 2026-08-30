using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DigitalWorkstation.Core.Models.Events;
using DigitalWorkstation.Core.Resource;

namespace DigitalWorkstation.DashBoard.ViewModels.Windows;

/// <summary>
///     启动台进度窗 ViewModel：订阅启动进度与模块失败事件，显示阶段名、当前模块名与 i/N；
///     模块失败时显示错误详情，"继续/退出"按钮把决策发布回启动序列
/// </summary>
public partial class DashBoardWindowViewModel : ObservableObject
{
    private readonly IEventAggregator _eventAggregator;

    public DashBoardWindowViewModel(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        eventAggregator.GetEvent<StartupProgressEvent>().Subscribe(OnProgress, ThreadOption.UIThread, true);
        eventAggregator.GetEvent<ModuleLoadFailedEvent>().Subscribe(OnModuleFailed, ThreadOption.UIThread, true);
    }

    [ObservableProperty]
    private string _phaseText = Language.SplashStartingText;

    /// <summary>
    ///     当前加载模块名与 i/N 进度；非加载模块阶段为空
    /// </summary>
    [ObservableProperty]
    private string _moduleText = string.Empty;

    [ObservableProperty]
    private bool _isFailed;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    private void OnProgress(StartupProgress progress)
    {
        IsFailed = false;
        PhaseText = progress.Phase switch
        {
            StartupPhase.CoreServices => Language.SplashPhaseCoreServices,
            StartupPhase.LoadingModules => Language.SplashPhaseLoadingModules,
            StartupPhase.Ready => Language.SplashPhaseReady,
            _ => PhaseText
        };
        ModuleText = progress.Phase == StartupPhase.LoadingModules
            ? FormatModuleText(progress.ModuleName, progress.ModuleIndex, progress.ModuleCount)
            : string.Empty;
    }

    private void OnModuleFailed(ModuleLoadFailure failure)
    {
        IsFailed = true;
        PhaseText = Language.SplashPhaseFailed;
        ModuleText = FormatModuleText(failure.ModuleName, failure.ModuleIndex, failure.ModuleCount);
        ErrorMessage = failure.ErrorMessage;
    }

    private static string FormatModuleText(string? moduleName, int index, int count)
    {
        return $"{moduleName}（{index}/{count}）";
    }

    /// <summary>
    ///     继续：跳过失败模块，加载其余模块并进入工作区
    /// </summary>
    [RelayCommand]
    private void Continue()
    {
        _eventAggregator.GetEvent<StartupFailureActionEvent>().Publish(StartupFailureAction.Continue);
    }

    /// <summary>
    ///     退出：终止应用
    /// </summary>
    [RelayCommand]
    private void Exit()
    {
        _eventAggregator.GetEvent<StartupFailureActionEvent>().Publish(StartupFailureAction.Exit);
    }
}
