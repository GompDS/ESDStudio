using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using ESDStudio.ViewModels;

namespace ESDStudio.Commands;

public class NewBNDCommand : CommandBase
{
    public NewBNDCommand(BNDViewModel bnd, ObservableCollection<BNDViewModel> bnds)
    {
        _bnd = bnd;
        _bnds = bnds;
    }
    
    private BNDViewModel _bnd;
    private ObservableCollection<BNDViewModel> _bnds;
    
    public override void Redo()
    {
        if (_bnd.Description.Length > 0 &&
            !Project.Current.MapDescriptions.Keys.Any(x => x.Equals(_bnd.Name)))
        {
            Project.Current.Game.MapDescriptions.TryGetValue(_bnd.Name, out string? gameDesc);
            if (gameDesc != _bnd.Description)
            {
                Project.Current.MapDescriptions.Add(_bnd.Name, _bnd.Description);
            }
            _bnd.DescriptionEditCount++;
        }

        _bnds.Add(_bnd);
        ICollectionView collectionView = CollectionViewSource.GetDefaultView(_bnds);
        collectionView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
    }

    public override void Undo()
    {
        if (_bnd.Description.Length > 0 &&
            Project.Current.Game.MapDescriptions.Keys.Any(x => x.Equals(_bnd.Name)))
        {
            Project.Current.MapDescriptions.Remove(_bnd.Name);
            _bnd.DescriptionEditCount--;
        }

        _bnds.Remove(_bnd);
    }
}