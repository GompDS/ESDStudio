using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using ESDStudio.Models;
using SoulsFormats;

namespace ESDStudio.ViewModels;

public class BNDViewModel : ViewModelBase
{
    public BNDViewModel()
    {
        Visibility = Visibility.Hidden;
        BNDModels = new ObservableCollection<BNDModel>();
    }
    public ObservableCollection<BNDModel> BNDModels { get; set; }
    
    private Visibility _visibility;
    public Visibility Visibility
    {
        get
        {
            return _visibility;
        }
        set
        {
            if (_visibility != value)
            {
                _visibility = value;
                OnPropertyChanged();
            }
        }
    }

    public void PopulateBNDModels(string modDirectory, string gameDirectory, GameInfo gameInfo)
    {
        List<string> modTalkBNDNames = new();
        IEnumerable<string> bndPaths = 
            Directory.EnumerateFiles(modDirectory + @"\script\talk", "*.talkesdbnd.dcx");
        foreach (string bndPath in bndPaths)
        {
            string bndName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(bndPath));
            modTalkBNDNames.Add(bndName);
            BNDModels.Add(new BNDModel(bndPath, gameInfo));
        }
            
        bndPaths = Directory.EnumerateFiles(gameDirectory + @"\script\talk", "*.talkesdbnd.dcx");
        foreach (string bndPath in bndPaths.Where(x => !modTalkBNDNames.Any(x.Contains)))
        {
            BNDModels.Add(new BNDModel(bndPath, gameInfo));
        }
        
        ICollectionView collectionView = CollectionViewSource.GetDefaultView(BNDModels);
        collectionView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
    }
}