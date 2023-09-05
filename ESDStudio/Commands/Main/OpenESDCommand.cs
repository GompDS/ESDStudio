using System.Windows;
using ESDStudio.ViewModels;
using ESDStudio.Views;

namespace ESDStudio.Commands.Main;

public class OpenESDCommand : CommandBase
{
    public OpenESDCommand(ESDViewModel esd)
    {
        _esd = esd;
        object? mainWindow = Application.Current.MainWindow;
        if (mainWindow != null)
        {
            MainWindowViewModel mainViewModel = (MainWindowViewModel)((MainWindow)mainWindow).DataContext;
            _oldEsd = mainViewModel.CurrentTab;
        }
    }
    
    private ESDViewModel _esd;
    private ESDViewModel? _oldEsd;
    
    public override void Redo()
    {
        object? mainWindow = Application.Current.MainWindow;
        if (mainWindow != null)
        {
            MainWindowViewModel mainViewModel = (MainWindowViewModel)((MainWindow)mainWindow).DataContext;
            if (mainViewModel.OpenTabs.Contains(_esd)) return;
            if (_esd.IsDecompiled == false)
            {
                _esd.Decompile();
            }
            _esd.Code.UndoStack.ClearAll();
            mainViewModel.OpenTabs.Add(_esd);
            mainViewModel.CurrentTab = _esd;
        }
    }

    public override void Undo()
    {
        object? mainWindow = Application.Current.MainWindow;
        if (mainWindow != null)
        {
            MainWindowViewModel mainViewModel = (MainWindowViewModel)((MainWindow)mainWindow).DataContext;
            mainViewModel.OpenTabs.Remove(_esd);
            mainViewModel.CurrentTab = _oldEsd;
        }
    }
}