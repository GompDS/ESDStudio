using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace ESDStudio.ViewModels;

public abstract class DialogViewModelBase : ViewModelBase
{
    public DialogViewModelBase()
    {
        ConfirmCommand = new RelayCommand(Confirm);
        CancelCommand = new RelayCommand(Cancel);
    }
    
    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }
    
    private bool? _dialogResult;
    public bool? DialogResult
    {
        get => _dialogResult;
        set
        {
            if (_dialogResult != value)
            {
                _dialogResult = value;
                OnPropertyChanged();
            }
        }
    }

    protected virtual void Confirm()
    {
        DialogResult = true;
    }
    
    protected virtual void Cancel()
    {
        DialogResult = false;
    }
}