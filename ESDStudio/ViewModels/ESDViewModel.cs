using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
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
        SaveCommand = new RelayCommand(Save, CanSave);
    }
    
    private ESDModel ESD;
    public BNDViewModel ParentViewModel;
    public ICommand EditIdCommand { get; }
    public ICommand EditDescriptionCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand PasteCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SaveCommand { get; }

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
            ((RelayCommand)SaveCommand).NotifyCanExecuteChanged();
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
            ((RelayCommand)SaveCommand).NotifyCanExecuteChanged();
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

    public void Decompile(string modDirectory, string gameDirectory)
    {
        BND4 parentBND = GetTalkBND(modDirectory, gameDirectory);
        BinderFile BNDFile = parentBND.Files.First(x => x.Name.EndsWith(Name + ".esd", StringComparison.OrdinalIgnoreCase));
        string cwd = AppDomain.CurrentDomain.BaseDirectory;
        string tempESDFile = cwd + $"esdtool\\{Name}.esd";
        File.WriteAllBytes(tempESDFile, BNDFile.Bytes);
        bool success = RunESDTool($"-i {Name}.esd -writepy %e.esd.py");
        File.Delete(tempESDFile);
        if (success == false) return;
        string tempPyFile = cwd + $"esdtool\\{Name}.esd.py";
        Code = File.ReadAllText(tempPyFile);
        foreach (FunctionDefinition funcDef in XmlData.FunctionDefinitions.
                     Where(x => x.Parameters.Any(y => y.IsEnum || y.Type == "bool") ||
                                x.ReturnValue is { Type: "enum" or "bool" }))
        {
            Code = funcDef.MakeNumberValuesDescriptive(Code);
        }
        File.Delete(tempPyFile);
        IsDecompiled = true;
    }

    private BND4 GetTalkBND(string modDirectory, string gameDirectory)
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
        return new BND4();
    }

    public bool Compile(GameInfo? game, string modDirectory, string gameDirectory)
    {
        if (game == null) return false;
        string codeCopy = Code;
        codeCopy = codeCopy.Replace("true", "1");
        codeCopy = codeCopy.Replace("false", "0");
        foreach (string enumType in XmlData.EnumTemplates.Keys)
        {
            foreach (Tuple<int,string> enumValuePair in XmlData.EnumTemplates[enumType])
            {
                codeCopy = codeCopy.Replace($"{enumType}.{enumValuePair.Item2}",
                    enumValuePair.Item1.ToString());
            }
        }
        string cwd = AppDomain.CurrentDomain.BaseDirectory;
        string tempPyFile = $"{cwd}\\esdtool\\{Name}.esd.py";
        File.WriteAllText(tempPyFile, codeCopy);
        bool success = RunESDTool($"-{game} " +
                                  $"-basedir \"{gameDirectory}\" " +
                                  $"-i \"{tempPyFile}\" -writeloose \"{cwd}esdtool\\{Name}.esd\"");
        File.Delete(tempPyFile);
        return success;
    }

    private bool RunESDTool(string arguments)
    {
        string cwd = AppDomain.CurrentDomain.BaseDirectory;
        Process esdtool = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = cwd + @"esdtool\esdtool.exe",
                Arguments = arguments,
                WorkingDirectory = cwd + "esdtool",
                RedirectStandardError = true,
                RedirectStandardOutput = true
            }
        };
        esdtool.Start();
        esdtool.WaitForExit();
        string stdout = esdtool.StandardOutput.ReadToEnd();
        string errorDescription = "ERROR:";
        string lineColumn = "-:-";
        string lineContents = "";
        int errorIndex = stdout.IndexOf("ERROR", StringComparison.Ordinal);
        if (errorIndex > -1)
        {
            int errorEnd = stdout.IndexOf('\r', errorIndex);
            errorDescription = stdout.Substring(errorIndex, errorEnd - errorIndex + 1);
            Match match = Regex.Match(stdout, @":[0-9]+:[0-9]+:");
            if (match.Success)
            {
                lineColumn = match.Value.Substring(1, match.Length - 2);
                int startIndex = match.Index + match.Length;
                int lineEndIndex = stdout.IndexOf('\r', startIndex);
                lineContents = stdout.Substring(startIndex, lineEndIndex - startIndex);
            }
        }
        string stderr = esdtool.StandardError.ReadToEnd();
        if (stderr.Length > 0)
        {
            ShowErrorMessageBox($"{errorDescription}\n({lineColumn}) \"{lineContents}\"\n" + stderr);
        }
        return stderr.Length == 0;
    }

    private void Save()
    {
        string cwd = AppDomain.CurrentDomain.BaseDirectory;
        MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;
        if (mainWindow == null) return;
        MainWindowViewModel? vm = mainWindow.DataContext as MainWindowViewModel;
        if (vm == null) return;
        if (vm.ProjectGame == null) return;
        if (IsESDEdited)
        {
            if (Code.Length > 0)
            {
                bool success = Compile(vm.ProjectGame, vm.ProjectModDirectory, vm.ProjectGameDirectory);
                if (success)
                {
                    BND4 bnd = GetTalkBND(vm.ProjectModDirectory, vm.ProjectGameDirectory);
                    BinderFile? file = bnd.Files.FirstOrDefault(x => x.Name.EndsWith($"{Name}.esd"));
                    string tempESDFile = $"{cwd}\\esdtool\\{Name}.esd";
                    if (file == null)
                    {
                        file = new BinderFile(Binder.FileFlags.Flag1, 
                            $"{vm.ProjectGame.FilePathStart}\\script\\talk\\{ParentViewModel.Name}\\{Name}.esd",
                            File.ReadAllBytes(tempESDFile));
                        for (int i = 0; i < bnd.Files.Count; i++)
                        {
                            if (string.Compare(bnd.Files[i].Name, file.Name, StringComparison.Ordinal) > 0)
                            {
                                bnd.Files.Insert(i, file);
                            }
                            bnd.Files[i].ID = i;
                        }
                    }
                    else
                    {
                        file.Bytes = File.ReadAllBytes($"{cwd}\\esdtool\\{Name}.esd");
                    }
                    File.Delete(tempESDFile);
                    bnd.Write($"{vm.ProjectModDirectory}\\script\\talk\\{ParentViewModel.Name}.talkesdbnd.dcx");
                    IsESDEdited = false;
                }
            }
        }
    }
    
    private bool CanSave()
    {
        return IsESDEdited || IsDescriptionEdited;
    }
}