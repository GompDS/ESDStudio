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
using ICSharpCode.AvalonEdit.Document;
using SoulsFormats;
using Tomlyn;
using Tomlyn.Model;

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
    
    public ESDModel ESD;
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
    
    public TextDocument Code
    {
        get 
        {
            return ESD.Code;
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

    public ESDViewModel? SourceESD = null;

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
        int newId = int.Parse(editIdViewModel.NewIdEntry);
        if (editIdView.DialogResult != true) return;
        if (IsDecompiled == false)
        {
            Decompile(ProjectData.ModDirectory, ProjectData.GameDirectory);
        }
        Code.Text = Code.Text.ReplaceMatches(@"(?<=t[0-9]{3})" + Regex.Escape($"{Id:D3}"),
            newId.ToString("D3"), true, false);
        Id = newId;
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
        object? mainWindow = Application.Current.MainWindow;
        if (mainWindow == null) return;
        MainWindowViewModel mainViewModel = (MainWindowViewModel)((MainWindow)mainWindow).DataContext;
        mainViewModel.CloseTab(this);
        ParentViewModel.ESDViewModels.Remove(this);
        ParentViewModel.BND.ESDModels.Remove(ESD);
    }

    public void Decompile(string modDirectory, string gameDirectory)
    {
        BND4 parentBND;
        string ESDSourceName;
        if (SourceESD != null)
        {
            ESDViewModel CopiedESD = GetCopiedESD();
            parentBND = CopiedESD.ParentViewModel.GetTalkBND(modDirectory, gameDirectory);
            ESDSourceName = CopiedESD.Name;
        }
        else
        {
            parentBND = ParentViewModel.GetTalkBND(modDirectory, gameDirectory);
            ESDSourceName = Name;
        }
        BinderFile BNDFile = parentBND.Files.First(x => x.Name.EndsWith(ESDSourceName + ".esd", StringComparison.OrdinalIgnoreCase));
        string cwd = AppDomain.CurrentDomain.BaseDirectory;
        string tempESDFile = cwd + $"esdtool\\{Name}.esd";
        File.WriteAllBytes(tempESDFile, BNDFile.Bytes);
        bool success = RunESDTool($"-i {Name}.esd -writepy %e.esd.py");
        File.Delete(tempESDFile);
        if (success == false) return;
        string tempPyFile = cwd + $"esdtool\\{Name}.esd.py";
        Code.Text = File.ReadAllText(tempPyFile);
        foreach (FunctionDefinition funcDef in XmlData.FunctionDefinitions.
                     Where(x => x.Parameters.Any(y => y.IsEnum || y.Type == "bool") ||
                                x.ReturnValue is { Type: "enum" or "bool" }))
        {
            Code.Text = funcDef.MakeNumberValuesDescriptive(Code.Text);
        }
        File.Delete(tempPyFile);
        IsDecompiled = true;
    }

    public bool Compile(GameInfo? game, string gameDirectory)
    {
        if (game == null) return false;
        string codeCopy = Code.Text;
        codeCopy = codeCopy.ReplaceMatches("true", "1", false, true);
        codeCopy = codeCopy.ReplaceMatches("false", "0", false, true);
        foreach (string enumType in XmlData.EnumTemplates.Keys)
        {
            foreach (Tuple<int,string> enumValuePair in XmlData.EnumTemplates[enumType])
            {
                codeCopy = codeCopy.ReplaceMatches($"{enumType}.{enumValuePair.Item2}",
                    enumValuePair.Item1.ToString(), false, true);
            }
        }
        string cwd = AppDomain.CurrentDomain.BaseDirectory;
        string tempPyFile = $"{cwd}\\esdtool\\{Name}.esd.py";
        File.WriteAllText(tempPyFile, codeCopy);
        bool success = RunESDTool($"-{game} " +
                                  $"-basedir \"{gameDirectory}\" " +
                                  $"-i \"{tempPyFile}\" -writeloose \"{cwd}esdtool\\{Name}.esd\"");
        if (Directory.Exists($"{ProjectData.BaseDirectory}\\{ParentViewModel.Name}") == false)
        {
            Directory.CreateDirectory($"{ProjectData.BaseDirectory}\\{ParentViewModel.Name}");
        }
        File.WriteAllText($"{ProjectData.BaseDirectory}\\{ParentViewModel.Name}\\{Name}.py", codeCopy);
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
            ShowErrorMessageBox($"{errorDescription}\n{Name} ({lineColumn}) \"{lineContents}\"\n" + stderr);
        }
        return stderr.Length == 0;
    }

    private void Save()
    {
        if (ProjectData.Game == null) return;
        if (IsESDEdited)
        {
            SaveESD();
        }
        
        if (IsDescriptionEdited)
        {
            SaveDescription();
        }
    }

    private void SaveESD()
    {
        string cwd = AppDomain.CurrentDomain.BaseDirectory;
        if (Code.TextLength > 0)
        {
            bool success = Compile(ProjectData.Game, ProjectData.GameDirectory);
            if (success)
            {
                BND4 bnd = ParentViewModel.GetTalkBND(ProjectData.ModDirectory, ProjectData.GameDirectory);
                BinderFile? file = bnd.Files.FirstOrDefault(x => x.Name.EndsWith($"{Name}.esd"));
                string tempESDFile = $"{cwd}\\esdtool\\{Name}.esd";
                if (file == null)
                {
                    file = new BinderFile(Binder.FileFlags.Flag1, 
                        $"{ProjectData.Game.FilePathStart}\\script\\talk\\{ParentViewModel.Name}\\{Name}.esd",
                        File.ReadAllBytes(tempESDFile));
                    for (int i = 0; i < bnd.Files.Count; i++)
                    {
                        if (string.Compare(bnd.Files[i].Name, file.Name, StringComparison.Ordinal) > 0)
                        {
                            bnd.Files.Insert(i, file);
                            break;
                        }
                        if (i == bnd.Files.Count - 1)
                        {
                            bnd.Files.Add(file);
                            break;
                        }
                    }

                    for (int i = 0; i < bnd.Files.Count; i++)
                    {
                        bnd.Files[i].ID = i;
                    }
                }
                else
                {
                    file.Bytes = File.ReadAllBytes($"{cwd}\\esdtool\\{Name}.esd");
                }
                File.Delete(tempESDFile);
                bnd.Write($"{ProjectData.ModDirectory}\\script\\talk\\{ParentViewModel.Name}.talkesdbnd.dcx");
                IsESDEdited = false;
            }
        }
    }

    private void SaveDescription()
    {
        /*ProjectData.ESDDescriptions.TryGetValue(ParentViewModel.Name, out Dictionary<int, string>? map);
        if (map == null)
        {
            map = new Dictionary<int, string>();
            ProjectData.ESDDescriptions.Add(ParentViewModel.Name, map);
        }

        map.TryGetValue(Id, out string? oldDescription);
        if (oldDescription == null)
        {
            map.Add(Id, Description);
        }
        else
        {
            map[Id] = Description;
        }*/
        IsDescriptionEdited = false;
        
        ProjectUtils.WriteESDDescriptions(ProjectData.ESDDescriptions, "ESDDescriptions",
            ProjectData.BaseDirectory + @"\ESDDescriptions.toml");
    }
    
    private bool CanSave()
    {
        return IsESDEdited || IsDescriptionEdited;
    }

    private ESDViewModel GetCopiedESD()
    {
        ESDViewModel currentESD = this;
        while (currentESD.SourceESD != null)
        {
            currentESD = currentESD.SourceESD;
        }

        return currentESD;
    }
}