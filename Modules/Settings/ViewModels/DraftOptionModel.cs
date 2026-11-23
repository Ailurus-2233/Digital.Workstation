using CommunityToolkit.Mvvm.ComponentModel;

namespace DigitalWorkstation.Settings.ViewModels;

public abstract class DraftOptionModel : ObservableObject
{
    public abstract bool HasDraft { get; }
    protected abstract void AcceptDraft();

    internal void FinishSave()
    {
        AcceptDraft();
        OnPropertyChanged(nameof(HasDraft));
    }
}
