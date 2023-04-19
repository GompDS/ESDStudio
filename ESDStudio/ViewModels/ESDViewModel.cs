using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.Input;
using ESDStudio.Models;
using ESDStudio.Views;
using SoulsFormats;

namespace ESDStudio.ViewModels;

public class ESDViewModel : ViewModelBase
{
    public ESDViewModel(ESDModel esdModel, BNDViewModel parent)
    {
        ESD = esdModel;
        ParentViewModel = parent;
        EditIdCommand = new RelayCommand(EditId);
        EditDescriptionCommand = new RelayCommand(EditDescription);
        CopyCommand = new RelayCommand(Copy);
        PasteCommand = new RelayCommand(Paste);
        DeleteCommand = new RelayCommand(Delete);
    }
    
    private ESDModel ESD;
    public BNDViewModel ParentViewModel;
    public ICommand EditIdCommand { get; }
    public ICommand EditDescriptionCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand PasteCommand { get; }
    public ICommand DeleteCommand { get; }

    public string Name
    {
        get
        {
            return ESD.Name;
        }
    }
    
    public int Id
    {
        get
        {
            return ESD.Id;
        }
        set
        {
            if (ESD.Id != value)
            {
                ESD.Id = value;
                OnPropertyChanged();
                OnPropertyChanged("Name");
                IsESDEdited = true;
            }
        }
    }
    
    public string Description
    {
        get 
        {
            return ESD.Description;
        }
        set
        {
            if (ESD.Description != value)
            {
                ESD.Description = value;
                OnPropertyChanged();
                IsDescriptionEdited = true;
            }
        }
    }
    
    public string Code
    {
        get 
        {
            return ESD.Code;
        }
        set
        {
            if (ESD.Code != value)
            {
                ESD.Code = value;
                OnPropertyChanged();
            }
        }
    }
    
    public bool IsESDEdited
    {
        get
        {
            return ESD.IsESDEdited;
        }
        set
        {
            ESD.IsESDEdited = value;
            OnPropertyChanged();
            ParentViewModel.UpdateIsBNDEdited();
        }
    }
    
    public bool IsDescriptionEdited
    {
        get
        {
            return ESD.IsDescriptionEdited;
        }
        set
        {
            ESD.IsDescriptionEdited = value;
            OnPropertyChanged();
            ParentViewModel.UpdateIsBNDEdited();
        }
    }
    
    public bool IsDecompiled
    {
        get
        {
            return ESD.IsDecompiled;
        }
        set
        {
            ESD.IsDecompiled = value;
            OnPropertyChanged();
        }
    }

    private void EditId()
    {
        EditESDIdViewModel editIdViewModel = new(Id, ParentViewModel.ESDViewModels.Select(x => x.Id).ToList());
        ESDEditIDView editIdView = new()
        {
            Owner = Application.Current.MainWindow,
            ShowInTaskbar = false,
            DataContext = editIdViewModel
        };
        editIdViewModel.LocalESDIds.Remove(Id);
        editIdView.ShowDialog();
        if (editIdView.DialogResult != true) return;
        Id = int.Parse(editIdViewModel.NewIdEntry);
        ParentViewModel.ESDViewModels = new ObservableCollection<ESDViewModel>(
            ParentViewModel.ESDViewModels.OrderBy(x => x.Id));
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
        Description = editDescriptionViewModel.NewDescriptionEntry;
    }
    
    private void Copy()
    {
        object? mainWindow = Application.Current.MainWindow;
        if (mainWindow == null) return;
        MainWindowViewModel mainViewModel = (MainWindowViewModel)((MainWindow)mainWindow).DataContext;
        mainViewModel.CopiedESDViewModel = this;
    }

    private void Paste()
    {
        ParentViewModel.PasteESDCommand.Execute(null);
    }

    private void Delete()
    {
        MessageBoxResult messageBoxResult =
            ShowConfirmationMessageBox($"Are you sure you want to delete {Name}?");
        if (messageBoxResult == MessageBoxResult.No) return;
        IsESDEdited = true;
        ParentViewModel.ESDViewModels.Remove(this);
    }

    public void DecompileESD(string modDirectory, string gameDirectory)
    {
        string talkPath = $"\\script\\talk\\{ParentViewModel.Name}.talkesdbnd.dcx";
        string basePath;
        if (File.Exists($"{modDirectory}\\{talkPath}"))
        {
            basePath = modDirectory;
        }
        else
        {
            basePath = gameDirectory;
        }
        string BNDPath = $"{basePath}\\{talkPath}";
        BND4 parentBND = BND4.Read(BNDPath);
        BinderFile BNDFile = parentBND.Files.First(x => x.Name.EndsWith(Name + ".esd", StringComparison.OrdinalIgnoreCase));
        string cwd = AppDomain.CurrentDomain.BaseDirectory;
        string tempESDFile = cwd + $"esdtool\\{Name}.esd";
        File.WriteAllBytes(tempESDFile, BNDFile.Bytes);
        Process esdtool = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = cwd + @"esdtool\esdtool.exe",
                Arguments = $"-i {Name}.esd -writepy %e.py",
                WorkingDirectory = cwd + "esdtool"
            }
        };
        esdtool.Start();
        esdtool.WaitForExit();
        string tempPyFile = cwd + $"esdtool\\{Name}.py";
        Code = File.ReadAllText(tempPyFile);
        foreach (FunctionDefinition funcDef in XmlData.FunctionDefinitions.
                     Where(x => x.Parameters.Any(y => y.IsEnum || y.Type == "bool") ||
                                x.ReturnValue is { Type: "enum" or "bool" }))
        {
            Code = funcDef.MakeNumberValuesDescriptive(Code);
        }
        File.Delete(tempESDFile);
        File.Delete(tempPyFile);
        IsDecompiled = true;
    }
}