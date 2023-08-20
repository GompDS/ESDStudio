using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using ESDStudio.Commands;
using ESDStudio.Models;
using ESDStudio.Views;
using SoulsFormats;

namespace ESDStudio.ViewModels;

public class BNDViewModel : ViewModelBase
{
    public BNDViewModel(string BNDPath, GameInfo gameInfo)
    {
        BND = new BNDModel(BNDPath, gameInfo);
        ESDViewModels = new ObservableCollection<ESDViewModel>();
        foreach (ESDModel esdModel in BND.ESDModels)
        {
            ESDViewModels.Add(new ESDViewModel(esdModel, this));
        }
        EditDescriptionCommand = new RelayCommand(EditDescription);
        NewESDCommand = new RelayCommand(NewESD);
        PasteESDCommand = new RelayCommand(PasteESD);
        SaveCommand = new RelayCommand(Save, CanSave);
    }
    
    public BNDViewModel(int mapId, int blockId, string description, GameInfo gameInfo)
    {
        BND = new BNDModel(mapId, blockId, description, gameInfo);
        ESDViewModels = new ObservableCollection<ESDViewModel>();
        EditDescriptionCommand = new RelayCommand(EditDescription);
        NewESDCommand = new RelayCommand(NewESD);
        PasteESDCommand = new RelayCommand(PasteESD);
        SaveCommand = new RelayCommand(Save);
    }
    public ICommand EditDescriptionCommand { get; }
    public ICommand NewESDCommand { get; }
    public ICommand PasteESDCommand { get; }
    public ICommand SaveCommand { get; }

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

    private ObservableCollection<ESDViewModel> _esdViewModels = new();
    public ObservableCollection<ESDViewModel> ESDViewModels
    {
        get
        {
            return _esdViewModels;
        }
        set
        {
            _esdViewModels = value;
            OnPropertyChanged();
        }
    }

    public BNDModel BND;
    
    public string Name
    {
        get
        {
            return BND.Name;
        }
        set
        {
            if (BND.Name != value)
            {
                BND.Name = value;
                OnPropertyChanged();
            }
        }
    }
    
    public string Description
    {
        get
        {
            return BND.Description;
        }
        set
        {
            if (BND.Description != value)
            {
                BND.Description = value;
                OnPropertyChanged();
                //IsDescriptionEdited = true;
                ((RelayCommand)SaveCommand).NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsBNDEdited
    {
        get
        {
            return BND.IsContentsEdited || LastSavedESDCount != ESDViewModels.Count;
        }
    }

    public int LastSavedESDCount
    {
        get
        {
            return BND.LastSavedESDCount;
        }
        set
        {
            if (value != BND.LastSavedESDCount)
            {
                BND.LastSavedESDCount = value;
                OnPropertyChanged("LastSavedEditCount");
                UpdateIsBNDEdited();
            }
        }
    }
    
    public bool IsDescriptionEdited => BND.IsDescriptionEdited;

    public int DescriptionEditCount
    {
        get
        {
            return BND.DescriptionEditCount;
        }
        set
        {
            if (value < 0) return;
            if ((BND.DescriptionEditCount == 0 && value > 0) || (BND.DescriptionEditCount > 0 && value == 0))
            {
                BND.DescriptionEditCount = value;
                OnPropertyChanged("IsDescriptionEdited");
                UpdateIsBNDEdited();
            }
            else
            {
                BND.DescriptionEditCount = value;
            }
        }
    }

    public void UpdateIsBNDEdited()
    {
        OnPropertyChanged("IsBNDEdited");
        ((RelayCommand)SaveCommand).NotifyCanExecuteChanged();
    }

    private void EditDescription()
    {
        EditDescriptionViewModel editDescriptionViewModel = new(Description);
        EditDescriptionView editDescriptionView = new()
        {
            Owner = Application.Current.MainWindow,
            ShowInTaskbar = false,
            DataContext = editDescriptionViewModel
        };
        editDescriptionView.ShowDialog();
        if (editDescriptionView.DialogResult != true) return;
        EditBNDDescriptionCommand command = new(this, editDescriptionViewModel.NewDescriptionEntry);
        command.Redo();
        MainWindowViewModel.UndoStack.Push(command);
        /*string newDescription = editDescriptionViewModel.NewDescriptionEntry;
        if (Description.Equals(newDescription) == false)
        {
            Description = newDescription;
            if (Project.Current.MapDescriptions.Keys.Contains(Name))
            {
                if (Description.Length > 0 || Project.Current.Game.MapDescriptions.Keys.Contains(Name))
                {
                    Project.Current.MapDescriptions[Name] = Description;
                }
                else
                {
                    Project.Current.MapDescriptions.Remove(Name);
                }

                DescriptionEditCount++;
            }
            else
            {
                Project.Current.Game.MapDescriptions.TryGetValue(Name, out string? defaultDescription);
                if (Description != defaultDescription)
                {
                    Project.Current.MapDescriptions.Add(Name, Description);
                    DescriptionEditCount++;
                }
            }
        }*/
    }

    private void NewESD()
    {
        BNDNewESDViewModel newESDViewModel = new(this);
        BNDNewESDView newESDView = new()
        {
            Owner = Application.Current.MainWindow,
            ShowInTaskbar = false,
            DataContext = newESDViewModel
        };
        newESDView.ShowDialog();
        if (newESDView.DialogResult != true) return;
        int id = int.Parse(newESDViewModel.IdEntry);
        ESDModel ESD = new ESDModel(id, newESDViewModel.DescriptionEntry, BND);
        ESDViewModel ESDViewModel = new ESDViewModel(ESD, this);
        ESDViewModels.Add(ESDViewModel);
        ESDViewModels = new ObservableCollection<ESDViewModel>(
            ESDViewModels.OrderBy(x => x.Id));
    }

    private void PasteESD()
    {
        object? mainWindow = Application.Current.MainWindow;
        if (mainWindow == null) return;
        MainWindowViewModel mainViewModel = (MainWindowViewModel)((MainWindow)mainWindow).DataContext;
        ESDViewModel? copiedESD = mainViewModel?.CopiedESDViewModel;
        if (copiedESD == null) return;
        ESDModel newESD = new ESDModel(copiedESD.ESD, BND);
        ESDViewModel newESDViewModel = new ESDViewModel(newESD, this)
        {
            SourceESD = copiedESD
        };
        int newId = newESDViewModel.Id;
        if (ESDViewModels.Any(x => x.Id == copiedESD.Id))
        {
            MessageBoxResult result = ShowErrorMessageBox("Another ESD with the same ID already exists in this map. Please specify a new ID.");
            if (result != MessageBoxResult.OK) return;
            EditESDIdViewModel editIdViewModel = new(copiedESD.Id, ESDViewModels.Select(x => x.Id).ToList());
            ESDEditIDView editIdView = new()
            {
                Owner = Application.Current.MainWindow,
                ShowInTaskbar = false,
                DataContext = editIdViewModel
            };
            editIdView.ShowDialog();
            if (editIdView.DialogResult != true) return;
            newId = int.Parse(editIdViewModel.NewIdEntry);
        }
        PasteESDCommand command = new(this, newESDViewModel, newId);
        command.Redo();
        MainWindowViewModel.UndoStack.Push(command);
    }

    private void Save()
    {
        SaveBNDCommand command = new(this);
        command.Redo();
        MainWindowViewModel.UndoStack.Push(command);
        /*if (IsDescriptionEdited)
        {
            DescriptionEditCount = 0;
        
            ProjectUtils.WriteMapDescriptions(Project.Current.MapDescriptions, "MapDescriptions",
                Project.Current.BaseDirectory + @"\MapDescriptions.toml");
        }
        
        foreach (ESDViewModel esd in ESDViewModels.Where(x => x.IsESDEdited))
        {
            esd.SaveCommand.Execute(null);
        }
        
        BND4 bnd = GetTalkBND(Project.Current.ModDirectory, Project.Current.GameDirectory);
        bnd.Files = bnd.Files.Where(x => 
            ESDViewModels.Any(y => y.Name == Path.GetFileNameWithoutExtension(x.Name))).ToList();
        bnd.Write($"{Project.Current.ModDirectory}\\script\\talk\\{Name}.talkesdbnd.dcx");
        UpdateIsBNDEdited();*/
    }
    
    private bool CanSave()
    {
        return (IsBNDEdited || IsDescriptionEdited || LastSavedESDCount != ESDViewModels.Count);
    }
    
    public BND4 GetTalkBND(string modDirectory, string gameDirectory, out string BNDPath)
    {
        string talkPath = $"\\script\\talk\\{Name}.talkesdbnd.dcx";
        string basePath;
        if (File.Exists($"{modDirectory}\\{talkPath}"))
        {
            basePath = modDirectory;
        }
        else
        {
            basePath = gameDirectory;
        }
        BNDPath = $"{basePath}\\{talkPath}";
        if (File.Exists(BNDPath))
        {
            if (BND4.IsRead(BNDPath, out BND4 bnd))
            {
                return bnd;
            }
        }

        BND4 newBND = new BND4
        {
            Compression = DCX.Type.DCX_DFLT_10000_44_9
        };
        return newBND;
    }
}