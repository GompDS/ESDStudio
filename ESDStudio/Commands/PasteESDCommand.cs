using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ESDStudio.ViewModels;
using ESDStudio.Views;

namespace ESDStudio.Commands;

public class PasteESDCommand : CommandBase
{
    public PasteESDCommand(BNDViewModel bnd, ESDViewModel esd, int newId)
    {
        _bnd = bnd;
        _esd = esd;
        _oldId = _esd.Id;
        _newId = newId;
    }
    
    private BNDViewModel _bnd;
    private ESDViewModel _esd;
    private int _oldId;
    private int _newId;

    public override void Redo()
    {
        if (_oldId != _newId)
        {
            _esd.Id = _newId;
            _esd.ESDEditCount++;
        }
        _esd.ESDEditCount ++;
        _bnd.ESDViewModels.Add(_esd);
        _bnd.BND.ESDModels.Add(_esd.ESD);
        _esd.Decompile(Project.Current.ModDirectory, Project.Current.GameDirectory);
        _bnd.ESDViewModels = new ObservableCollection<ESDViewModel>(_bnd.ESDViewModels.OrderBy(x => x.Id));
        _bnd.UpdateIsBNDEdited();
    }

    public override void Undo()
    {
        if (_oldId != _newId)
        {
            _esd.Id = _oldId;
            _esd.ESDEditCount--;
        }
        _esd.ESDEditCount -= 2;
        _bnd.ESDViewModels.Remove(_esd);
        _bnd.BND.ESDModels.Remove(_esd.ESD);
        _esd.Code.Text = "";
        _esd.IsDecompiled = false;
        _bnd.ESDViewModels = new ObservableCollection<ESDViewModel>(_bnd.ESDViewModels.OrderBy(x => x.Id));
        _bnd.UpdateIsBNDEdited();
        object? mainWindow = Application.Current.MainWindow;
        if (mainWindow != null)
        {
            MainWindowViewModel mainViewModel = (MainWindowViewModel)((MainWindow)mainWindow).DataContext;
            if (mainViewModel.OpenTabs.Contains(_esd))
            {
                mainViewModel.CloseTab(_esd);
            }
        }
    }
}