using System.Windows;
using ESDStudio.ViewModels;
using ESDStudio.Views;

namespace ESDStudio.Commands.Main;

public class CloseESDCommand : CommandBase
{
    public CloseESDCommand(ESDViewModel esd, bool tabWasOpen, int tabIndex)
    {
        _esd = esd;
        _tabWasOpen = tabWasOpen;
        _tabIndex = tabIndex;
    }
    
    private ESDViewModel _esd;
    private bool _tabWasOpen;
    private int _tabIndex;
    
    public override void Redo()
    {
        if (_tabWasOpen)
        {
            object? mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                MainWindowViewModel mainViewModel = (MainWindowViewModel)((MainWindow)mainWindow).DataContext;
                mainViewModel.CloseTab(_esd);
            }
        }
    }

    public override void Undo()
    {
        if (_tabWasOpen)
        {
            object? mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                MainWindowViewModel mainViewModel = (MainWindowViewModel)((MainWindow)mainWindow).DataContext;
                mainViewModel.OpenTabs.Insert(_tabIndex, _esd);
                mainViewModel.CurrentTab = _esd;
            }
        }
    }
}