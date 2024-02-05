using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using ESDStudio.Views;

namespace ESDStudio.ViewModels;

public class ESDGroupEditViewModel : ViewModelBase
{
    public ESDGroupEditViewModel(ESDGroupViewModel esdGroup)
    {
        ESDGroupViewModel = esdGroup;
        AddESDCommand = new RelayCommand(AddESD);
        SaveChangesCommand = new RelayCommand(SaveChanges);
    }
    
    public ICommand AddESDCommand { get; }
    public ICommand SaveChangesCommand { get; }

    private ESDGroupViewModel _esdGroupViewModel;
    public ESDGroupViewModel ESDGroupViewModel
    {
        get
        {
            return _esdGroupViewModel;
        }
        set
        {
            _esdGroupViewModel = value;
            OnPropertyChanged();
        }
    }
    
    private string _addESDIdEntry;
    public string AddESDIdEntry
    {
        get
        {
            return _addESDIdEntry;
        }
        set
        {
            _addESDIdEntry = value;
            OnPropertyChanged();
        }
    }

    public void AddESD()
    {
        if (AddESDIdEntry.Length == 0)
        {
            MessageBoxResult result = ShowErrorMessageBox("ESD ID must be at least 1 digit long");
            if (result == MessageBoxResult.OK) return;
        }

        int id = int.Parse(AddESDIdEntry);
        ESDGroupViewModel.AddESDCommand.Execute(id);
    }
    public void SaveChanges()
    {
        ESDGroupViewModel.SaveCommand.Execute(null);
    }
}