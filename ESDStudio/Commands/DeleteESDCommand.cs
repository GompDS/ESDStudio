using System;
using System.Windows;
using System.Windows.Input;
using ESDStudio.ViewModels;
using ESDStudio.Views;
using SoulsFormats;

namespace ESDStudio.Commands;

public class DeleteESDCommand : CommandBase
{
    public DeleteESDCommand(ESDViewModel esdToDelete, int esdIndex, bool tabWasOpen, int tabIndex)
    {
        _esd = esdToDelete;
        _indexInBnd = esdIndex;
        _tabWasOpen = tabWasOpen;
        _tabIndex = tabIndex;
    }

    private ESDViewModel _esd;
    private int _indexInBnd;
    private bool _tabWasOpen;
    private int _tabIndex;

    public override void Redo()
    {
        _esd.ESDEditCount++;
        if (_tabWasOpen)
        {
            object? mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                MainWindowViewModel mainViewModel = (MainWindowViewModel)((MainWindow)mainWindow).DataContext;
                mainViewModel.CloseTab(_esd);
            }
        }
        _esd.ParentViewModel.ESDViewModels.Remove(_esd);
        _esd.ParentViewModel.BND.ESDModels.Remove(_esd.ESD);
    }

    public override void Undo()
    {
        _esd.ESDEditCount--;
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
        _esd.ParentViewModel.ESDViewModels.Insert(_indexInBnd, _esd);
        _esd.ParentViewModel.BND.ESDModels.Insert(_indexInBnd, _esd.ESD);
    }
}