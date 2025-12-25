using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.Input;
using ESDStudio.Commands;
using ESDStudio.Commands.Main;
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
    
    public string Id
    {
        get => ESD.Id;
        set
        {
            if (ESD.Id != value)
            {
                ESD.Id = value;
                OnPropertyChanged();
                OnPropertyChanged("Name");
                ESDEditCount++;
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
                //IsDescriptionEdited = true;
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
    }

    public int ESDEditCount
    {
        get
        {
            return ESD.ESDEditCount;
        }
        set
        {
            if (value < 0) return;
            if ((ESD.ESDEditCount == 0 && value > 0) || (ESD.ESDEditCount > 0 && value == 0))
            {
                ESD.ESDEditCount = value;
                OnPropertyChanged("IsESDEdited");
                ParentViewModel.UpdateIsBNDEdited();
                ((RelayCommand)SaveCommand).NotifyCanExecuteChanged();
            }
            else
            {
                ESD.ESDEditCount = value;
            }
        }
    }
    
    public bool IsDescriptionEdited
    {
        get
        {
            return ESD.IsDescriptionEdited;
        }
    }
    
    public int DescriptionEditCount
    {
        get
        {
            return ESD.DescriptionEditCount;
        }
        set
        {
            if (value < 0) return;
            if ((ESD.DescriptionEditCount == 0 && value > 0) || (ESD.DescriptionEditCount > 0 && value == 0))
            {
                ESD.DescriptionEditCount = value;
                OnPropertyChanged("IsDescriptionEdited");
                ParentViewModel.UpdateIsBNDEdited();
                ((RelayCommand)SaveCommand).NotifyCanExecuteChanged();
            }
            else
            {
                ESD.DescriptionEditCount = value;
            }
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
    public ESDGroupViewModel? ESDGroup = null;

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
        EditESDIdCommand command = new(this, editIdViewModel.NewIdEntry);
        command.Redo();
        MainWindowViewModel.UndoStack.Push(command);
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
        if (editDescriptionViewModel.NewDescriptionEntry != Description)
        {
            EditESDDescriptionCommand command = new(this, editDescriptionViewModel.NewDescriptionEntry);
            command.Redo();
            MainWindowViewModel.UndoStack.Push(command);
        }
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
            ShowConfirmationMessageBox($"Are you sure you want to delete {Id}?");
        if (messageBoxResult == MessageBoxResult.No) return;
        object? mainWindow = Application.Current.MainWindow;
        if (mainWindow == null) return;
        MainWindowViewModel mainViewModel = (MainWindowViewModel)((MainWindow)mainWindow).DataContext;
        DeleteESDCommand command = new DeleteESDCommand(this, ParentViewModel.ESDViewModels.IndexOf(this),
            mainViewModel.OpenTabs.Contains(this), mainViewModel.OpenTabs.IndexOf(this));
        command.Redo();
        MainWindowViewModel.UndoStack.Push(command);
    }

    public void Decompile()
    {
        string decompiledCode = "";
        string pyFile = $"{Project.Current.BaseDirectory}\\{ParentViewModel.Name}\\{ESD.Id}.py";
        if (File.Exists(pyFile))
        {
            decompiledCode = File.ReadAllText(pyFile);
        }
        else
        {
            if (!Directory.Exists($"{Project.Current.ModDirectory}\\{Project.Current.Game.TalkPath}"))
            {
                ShowErrorMessageBox("Mod directory not found: " +
                                    $"\"{Project.Current.ModDirectory}\\{Project.Current.Game.TalkPath}\"");
                return;
            }
            if (!Directory.Exists($"{Project.Current.GameDirectory}\\{Project.Current.Game.TalkPath}"))
            {
                ShowErrorMessageBox("Game directory not found: " +
                                    $"\"{Project.Current.GameDirectory}\\{Project.Current.Game.TalkPath}\"");
                return;
            }

            decompiledCode = DecompileFromESD();
            if (decompiledCode == "") return;
        }

        Code.Text = decompiledCode;
        /*foreach (FunctionDefinition funcDef in Project.Current.Game.FunctionDefinitions.
                     Where(x => x.Parameters.Any(y => y.IsEnum || y.Type == "bool") ||
                                x.ReturnValue is { Type: "enum" or "bool" }))
        {
            Code.Text = funcDef.MakeNumberValuesDescriptive(Code.Text);
        }*/

        //Code.Text = Code.Text.Replace("    ", "\t");
        //.Text = Code.Text.ReplaceMatches("    ", "\t", false, false);
        //File.Delete(tempPyFile);
        IsDecompiled = true;
    }

    public string DecompileFromESD()
    {
        IBinder parentBND;
        string ESDSourceName;
        if (SourceESD != null)
        {
            ESDViewModel CopiedESD = GetCopiedESD();
            parentBND = CopiedESD.ParentViewModel.GetTalkBND(Project.Current.ModDirectory, Project.Current.GameDirectory, out string BNDPath);
            ESDSourceName = CopiedESD.Id;
        }
        else
        {
            parentBND = ParentViewModel.GetTalkBND(Project.Current.ModDirectory, Project.Current.GameDirectory, out string BNDPath);
            ESDSourceName = Id;
        }
        BinderFile BNDFile = parentBND.Files.First(x => x.Name.EndsWith(ESDSourceName + ".esd", StringComparison.OrdinalIgnoreCase));
        string cwd = AppDomain.CurrentDomain.BaseDirectory;
        string tempESDFile = cwd + $"esdtool\\{Id}.esd";
        File.WriteAllBytes(tempESDFile, BNDFile.Bytes);
        string generalSettings =
            $"-{Project.Current.Game.Name} -basedir \"{Project.Current.GameDirectory}\" -moddir \"{Project.Current.ModDirectory}\"";
        bool success = RunESDTool($"{generalSettings} {(!Editor.UseGameDataFlags ? "-noannotate " : "")} -i {Id}.esd -writepy %e.esd.py");
        File.Delete(tempESDFile);
        if (success == false) return "";
        string tempPyFile = cwd + $"esdtool\\{Id}.esd.py";
        string fileText = File.ReadAllText(tempPyFile);
        File.Delete(tempPyFile);
        return fileText;
    }

    public bool Compile(GameInfo? game, string gameDirectory)
    {
        if (game == null) return false;
        string codeCopy = Code.Text;
        /*codeCopy = codeCopy.ReplaceMatches("true", "1", false, true);
        codeCopy = codeCopy.ReplaceMatches("false", "0", false, true);
        codeCopy = codeCopy.ReplaceMatches("\t", "    ", false, false);
        foreach (string enumType in Project.Current.Game.EnumTemplates.Keys)
        {
            foreach (Tuple<int,string> enumValuePair in Project.Current.Game.EnumTemplates[enumType])
            {
                codeCopy = codeCopy.ReplaceMatches($"{enumType}.{enumValuePair.Item2}",
                    enumValuePair.Item1.ToString(), false, true);
            }
        }*/
        string cwd = AppDomain.CurrentDomain.BaseDirectory;
        string tempPyFile = $"{cwd}" + $"esdtool\\{Id}.esd.py";
        File.WriteAllText(tempPyFile, codeCopy);
        bool success = RunESDTool($"-{game} " +
                                  $"-basedir \"{gameDirectory}\" -moddir \"{Project.Current.ModDirectory}\" -esddir \"{gameDirectory}\\{Project.Current.Game.TalkPath}\" " +
                                  $"-i \"{tempPyFile}\" -writeloose \"{cwd}esdtool\\{Id}.esd\"");
        if (Directory.Exists($"{Project.Current.BaseDirectory}\\{ParentViewModel.Name}") == false)
        {
            Directory.CreateDirectory($"{Project.Current.BaseDirectory}\\{ParentViewModel.Name}");
        }

        if (success)
        {
            File.WriteAllText($"{Project.Current.BaseDirectory}\\{ParentViewModel.Name}\\{Id}.py", codeCopy);
        }
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
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        esdtool.OutputDataReceived += EsdtoolOnOutputDataReceived;
        esdtool.ErrorDataReceived += EsdtoolOnErrorDataReceived;
        outputString = "";
        errorString = "";
        esdtool.Start();
        esdtool.BeginOutputReadLine();
        esdtool.BeginErrorReadLine();
        //Task t1 = ConsumeReader(esdtool.StandardOutput);
        //Task t2 = ConsumeReader(esdtool.StandardError);
        esdtool.WaitForExit();
        esdtool.CancelOutputRead();
        esdtool.CancelErrorRead();
        //string stdout = esdtool.StandardOutput.ReadToEnd();
        string stdout = outputString;
        List<string> errors = new();
        int nextError = stdout.IndexOf("ERROR", StringComparison.Ordinal);
        while (nextError > -1 && nextError < stdout.Length)
        {
            int errorEnd = stdout.IndexOf('\r', nextError);
            string errorDescription = stdout.Substring(nextError, errorEnd - nextError + 1);
            Match match = Regex.Match(stdout.Substring(errorEnd), @":[0-9]+:[0-9]+:");
            if (match.Success)
            {
                string lineColumn = match.Value.Substring(1, match.Length - 2);
                int startIndex = match.Index + errorEnd + match.Length;
                int lineEndIndex = stdout.IndexOf('\r', startIndex);
                string lineContents = stdout.Substring(startIndex, lineEndIndex - startIndex);
                errors.Add($"{errorDescription}\n{Id} ({lineColumn}) \"{lineContents}\"\n");
                nextError = stdout.IndexOf("ERROR", lineEndIndex, StringComparison.Ordinal);
            }
            else
            {
                break;
            }
        }
        string stderr = errorString;
        if (stderr.Length > 0)
        {
            string message = $"Errors were encountered when attempting to compile {Id}";
            if (Description.Length > 0)
            {
                message += $" \"{Description}\"";
            }
            message += ".\r\n\r\n";
            foreach (string error in errors)
            {
                message += error;
            }

            message += stderr;
            ShowErrorMessageBox(message);
        }
        return stderr.Length == 0;
    }

    private void EsdtoolOnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data != null)
        {
            errorString += e.Data + "\r\n";
        }
    }

    private string errorString = ";";
    
    private string outputString = ";";
    
    private void EsdtoolOnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data != null)
        {
            outputString += e.Data + "\r\n";
        }
    }

    private void Save()
    {
        if (IsESDEdited)
        {
            SaveESD();
            ESDGroup?.SaveMembers(Code.Text, Id);
        }
        
        if (IsDescriptionEdited)
        {
            SaveDescription();
        }
    }

    public void SaveESD()
    {
        if (!Directory.Exists($"{Project.Current.ModDirectory}\\{Project.Current.Game.TalkPath}"))
        {
            ShowErrorMessageBox("Mod directory not found: " +
                                $"\"{Project.Current.ModDirectory}\\{Project.Current.Game.TalkPath}\"");
            return;
        }
        if (!Directory.Exists($"{Project.Current.GameDirectory}\\{Project.Current.Game.TalkPath}"))
        {
            ShowErrorMessageBox("Game directory not found: " +
                                $"\"{Project.Current.GameDirectory}\\{Project.Current.Game.TalkPath}\"");
            return;
        }
        
        string cwd = AppDomain.CurrentDomain.BaseDirectory;
        if (Code.TextLength > 0)
        {
            bool success = Compile(Project.Current.Game, Project.Current.GameDirectory);
            if (success)
            {
                IBinder bnd = ParentViewModel.GetTalkBND(Project.Current.ModDirectory, Project.Current.GameDirectory, out string BNDPath);
                if (!File.Exists(BNDPath + ".bak"))
                {
                    string writeBakPath = $"{Project.Current.ModDirectory}\\{Project.Current.Game.TalkPath}\\{ParentViewModel.Name}.talkesdbnd";
                    if (Project.Current.Game.Compression is not DCX.NoCompressionInfo)
                    {
                        writeBakPath += ".dcx";
                    }

                    writeBakPath += ".bak";
                    if (Project.Current.Game.BNDVersion == GameInfo.BNDType.BND3)
                    {
                        ((BND3)bnd).Write(writeBakPath);
                    }
                    else
                    {
                        ((BND4)bnd).Write(writeBakPath);
                    }
                }
                BinderFile? file = bnd.Files.FirstOrDefault(x => x.Name.EndsWith($"{Id}.esd"));
                string tempESDFile = $"{cwd}\\esdtool\\{Id}.esd";
                if (file == null)
                {
                    string internalPath = $"{Project.Current.Game.FilePathStart}\\{Project.Current.Game.TalkPath}\\";
                    if (Project.Current.Game.Type is not GameInfo.Game.DarkSouls or GameInfo.Game.DarkSoulsRemastered)
                    {
                        internalPath += $"{ParentViewModel.Name}\\";
                    }

                    internalPath += $"{Id}.esd";
                    
                    file = new BinderFile(Binder.FileFlags.Flag1, 
                        internalPath,
                        File.ReadAllBytes(tempESDFile));
                    if (bnd.Files.Count == 0)
                    {
                        bnd.Files.Add(file);
                    }
                    else
                    {
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
                    }

                    for (int i = 0; i < bnd.Files.Count; i++)
                    {
                        bnd.Files[i].ID = i;
                    }
                }
                else
                {
                    file.Bytes = File.ReadAllBytes($"{cwd}\\esdtool\\{Id}.esd");
                }
                File.Delete(tempESDFile);
                string writePath =
                    $"{Project.Current.ModDirectory}\\{Project.Current.Game.TalkPath}\\{ParentViewModel.Name}.talkesdbnd";
                if (Project.Current.Game.Compression is not DCX.NoCompressionInfo)
                {
                    writePath += ".dcx";
                }

                if (Project.Current.Game.BNDVersion == GameInfo.BNDType.BND3)
                {
                    ((BND3)bnd).Write(writePath);
                }
                else
                {
                    ((BND4)bnd).Write(writePath);
                }
                ESDEditCount = 0;
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
        DescriptionEditCount = 0;
        
        ProjectUtils.WriteESDDescriptions(Project.Current.ESDDescriptions, "ESDDescriptions",
            Project.Current.BaseDirectory + @"\ESDDescriptions.toml");
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