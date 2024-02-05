using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml;
using CommunityToolkit.Mvvm.Input;
using ESDStudio.Commands;
using ESDStudio.Commands.Main;
using ESDStudio.Models;
using ESDStudio.UserControls;
using ESDStudio.Views;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using SoulsFormats;
using SoulsFormats.Kuon;
using Tomlyn;
using Tomlyn.Model;
// ReSharper disable MemberCanBePrivate.Global

namespace ESDStudio.ViewModels;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        RecentProjects = new ObservableCollection<Project>();
        BNDViewModels = new ObservableCollection<BNDViewModel>();
        OpenTabs = new ObservableCollection<ESDViewModel>();
        ESDGroups = new ObservableCollection<ESDGroupViewModel>();
        //ESDGroups.Add(new ESDGroupViewModel("Bonfire"));
        UndoCommand = new RelayCommand(Undo);
        RedoCommand = new RelayCommand(Redo);
        OpenProjectCommand = new RelayCommand(OpenProject);
        NewProjectCommand = new RelayCommand(NewProject);
        NewBNDCommand = new RelayCommand(NewBND, CanMakeNewBND);
        CloseTabCommand = new RelayCommand(CloseCurrentTab, CanCloseTab);
        FindCommand = new RelayCommand(Find, CanCloseTab);
        ReplaceCommand = new RelayCommand(Replace, CanCloseTab);
        OpenRecentProjectCommand = new RelayCommand(OpenRecentProject);
        SaveCommand = new RelayCommand(Save);
        SaveAllCommand = new RelayCommand(SaveAll);
        ShowAboutCommand = new RelayCommand(ShowAbout);
        ShowEditorSettingsCommand = new RelayCommand(ShowEditorSettings);
        LoadEditorSettings();
        GetRecentProjects();
        if (RecentProjects.Count > 0)
        {
            LoadProject(RecentProjects[0]);
        }
    }

    public static Stack<CommandBase> UndoStack = new();
    public static Stack<CommandBase> RedoStack = new();
    
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public ICommand OpenProjectCommand { get; }
    public ICommand NewProjectCommand { get; }
    public ICommand OpenRecentProjectCommand { get; }
    public ICommand NewBNDCommand { get; }
    public ICommand CloseTabCommand { get; }
    public ICommand FindCommand { get; }
    public ICommand ReplaceCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand SaveAllCommand { get; }
    public ICommand ShowAboutCommand { get; }
    public ICommand ShowEditorSettingsCommand { get; }
    
    public ObservableCollection<Project> RecentProjects { get; }

    public string ProjectName
    {
        get
        {
            return Project.Current.Name;
        }
    }
    
    public ObservableCollection<BNDViewModel> BNDViewModels { get; }
    private object? _selectedTreeItem;
    public object? SelectedTreeItem
    {
        get
        {
            return _selectedTreeItem;
        }
        set
        {
            _selectedTreeItem = value;
            OnPropertyChanged();
        }
    }
    public ESDViewModel? CopiedESDViewModel { get; set; }
    public object? SelectedRecentProject { get; set; }
    public ObservableCollection<ESDViewModel> OpenTabs { get; }
    private ESDViewModel? _currentTab;
    public ESDViewModel? CurrentTab
    {
        get
        {
            return _currentTab;
        }
        set
        {
            ((RelayCommand)CloseTabCommand).NotifyCanExecuteChanged();
            ((RelayCommand)FindCommand).NotifyCanExecuteChanged();
            _currentTab = value;
            OnPropertyChanged();
        }
    }
    
    public ObservableCollection<ESDGroupViewModel> ESDGroups { get; }

    private void GetRecentProjects()
    {
        string tomlPath = AppDomain.CurrentDomain.BaseDirectory + "RecentProjects.toml";
        if (File.Exists(tomlPath))
        {
            TomlTable model = Toml.ToModel(File.ReadAllText(tomlPath), tomlPath);
            model.TryGetValue("recent_projects", out object obj);
            TomlTable projectsModel = (TomlTable)obj;
            foreach (string projectName in projectsModel.Keys)
            {
                projectsModel.TryGetValue(projectName, out object pathObj);
                string projectPath = (string)pathObj;
                if (File.Exists(projectPath) == false) continue;
                RecentProjects.Add(Project.ReadFromToml(projectPath));
            }
        }
    }

    private void AddCurrentProjectToRecent()
    {
        for (int i = 5; i <= RecentProjects.Count;)
        {
            RecentProjects.Remove(RecentProjects[i-1]);
        }

        if (RecentProjects.Any(x => x.Name.Equals(ProjectName)))
        {
            RecentProjects.Remove(RecentProjects.First(x => x.Name.Equals(ProjectName)));
        }
        RecentProjects.Insert(0, Project.Current);
        TomlTable model = new TomlTable();
        model.Add("Title", "RecentProjects");
        TomlTable subModel = new TomlTable();
        foreach (Project project in RecentProjects)
        {
            subModel.Add(project.Name, project.BaseDirectory + @"\ESDStudioProject.toml");
        }
        model.Add("recent_projects", subModel);
        File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "RecentProjects.toml", Toml.FromModel(model));
    }

    private void OpenProject()
    {
        OpenFileDialog fileDialog = new()
        {
            Filter = "TOML files (*.toml)|*.toml",
            InitialDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}Projects"
        };
        if (fileDialog.ShowDialog() == true)
        {
            LoadProject(Project.ReadFromToml(fileDialog.FileName));
        }
    }

    private void LoadProject(Project project)
    {
        // Copy Oodle
        if (project.Game.Type is GameInfo.Game.EldenRing or GameInfo.Game.Sekiro)
        {
            string gameOodlePath = project.GameDirectory + @"\oo2core_6_win64.dll";
            string localOodlePath = AppDomain.CurrentDomain.BaseDirectory + "oo2core_6_win64.dll";
            if (!File.Exists(localOodlePath))
            {
                try
                {
                    File.Copy(gameOodlePath, localOodlePath);
                }
                catch (Exception e)
                {
                    ShowErrorMessageBox(e.Message);
                    return;
                }
            }
            string esdtoolOodlePath = AppDomain.CurrentDomain.BaseDirectory + @"esdtool\oo2core_6_win64.dll";
            if (!File.Exists(esdtoolOodlePath))
            {
                try
                {
                    File.Copy(gameOodlePath, esdtoolOodlePath);
                }
                catch (Exception e)
                {
                    ShowErrorMessageBox(e.Message);
                    return;
                }
            }
            
            string esdtool_er_OodlePath = AppDomain.CurrentDomain.BaseDirectory + @"esdtool_er\oo2core_6_win64.dll";
            if (!File.Exists(esdtool_er_OodlePath))
            {
                try
                {
                    File.Copy(gameOodlePath, esdtool_er_OodlePath);
                }
                catch (Exception e)
                {
                    ShowErrorMessageBox(e.Message);
                    return;
                }
            }
        }
        Project.IsProjectLoaded = true;
        BNDViewModels.Clear();
        OpenTabs.Clear();
        ESDGroups.Clear();
        Project.Current = project;
        OnPropertyChanged("ProjectName");
        ShowBNDControl();
        if (Directory.Exists(project.BaseDirectory + @"\Groups"))
        {
            foreach (string groupPath in Directory.EnumerateFiles(project.BaseDirectory + @"\Groups", "*.toml"))
            {
                TomlTable model = Toml.ToModel(File.ReadAllText(groupPath), groupPath);
                model.TryGetValue("group_info", out object obj);
                TomlTable groupInfo = (TomlTable)obj;
                groupInfo.TryGetValue("name", out object nameObj);
                ESDGroupViewModel esdGroup = new ESDGroupViewModel((string)nameObj);
                groupInfo.TryGetValue("members", out object membersObj);
                foreach (long? memberId in (TomlArray)membersObj)
                {
                    if (memberId == null) continue;
                    //int id = int.Parse(memberId);
                    foreach (ESDViewModel esdViewModel in 
                             BNDViewModels.SelectMany(x => x.ESDViewModels.Where(y => y.Id == memberId)))
                    {
                        if (!esdGroup.Members.Contains(esdViewModel))
                        {
                            esdGroup.Members.Add(esdViewModel);
                            if (esdViewModel.ESDGroup != null)
                            {
                                esdViewModel.ESDGroup.Members.Remove(esdViewModel);
                            }
                            esdViewModel.ESDGroup = esdGroup;
                        }
                    }
                }
                ESDGroups.Add(esdGroup);
            }
        }
        AddCurrentProjectToRecent();
    }

    private void NewProject()
    {
        MainWindowNewProjectViewModel newProjectVM = new();
        MainWindowNewProjectView newProjectView = new()
        {
            Owner = Application.Current.MainWindow,
            ShowInTaskbar = false,
            DataContext = newProjectVM
        };
        newProjectView.ShowDialog();
        if (newProjectView.DialogResult != true) return;
        string baseDir = newProjectVM.ProjectBaseDirectoryEntry + $"\\{newProjectVM.ProjectNameEntry}";
        string? gameDir = Path.GetDirectoryName(newProjectVM.GameExecutableEntry);
        if (gameDir == null) return;
        if (newProjectVM.GameExecutableEntry.EndsWith("eboot.bin"))
        {
            gameDir += @"\dvdroot_ps4";
        }
        Project newProject = new(newProjectVM.ProjectNameEntry, baseDir, gameDir, 
            newProjectVM.ProjectModDirectoryEntry, newProjectVM.GameExecutableEntry);

        TomlTable model = new() { { "title", "ESDStudioProject" } };
        TomlTable projectInfo = new()
        {
            { "name", newProject.Name },
            { "base_directory", newProject.BaseDirectory },
            { "game_directory", newProject.GameDirectory },
            { "mod_directory", newProject.ModDirectory },
            { "game", newProject.Game.ToString() }
        };
        model.Add("project_info", projectInfo);

        if (!Directory.Exists(baseDir))
        {
            Directory.CreateDirectory(baseDir);
        }
        File.WriteAllText(newProject.BaseDirectory + @"\ESDStudioProject.toml", Toml.FromModel(model));
        LoadProject(newProject);
    }

    private void OpenRecentProject()
    {
        if (SelectedRecentProject == null) return;
        MenuItem item = (MenuItem)SelectedRecentProject;
        LoadProject((Project)item.DataContext);
    }
    
    private void PopulateBNDViewModels(string modDirectory, string gameDirectory, GameInfo gameInfo)
    {
        string searchPattern = $"*.talkesdbnd";
        if (Project.Current.Game.Compression != DCX.Type.None)
        {
            searchPattern += ".dcx";
        }
        
        List<string> modTalkBNDNames = new();
        if (Directory.Exists(modDirectory + $"\\{Project.Current.Game.TalkPath}"))
        {
            IEnumerable<string> bndPaths = 
                Directory.EnumerateFiles(modDirectory + $"\\{Project.Current.Game.TalkPath}", searchPattern);
            foreach (string bndPath in bndPaths)
            {
                string bndName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(bndPath));
                modTalkBNDNames.Add(bndName);
                BNDViewModels.Add(new BNDViewModel(bndPath, gameInfo));
            }
        }

        if (Directory.Exists(gameDirectory + $"\\{Project.Current.Game.TalkPath}"))
        {
            IEnumerable<string> bndPaths =
                Directory.EnumerateFiles(gameDirectory + $"\\{Project.Current.Game.TalkPath}", searchPattern);
            foreach (string bndPath in bndPaths.Where(x => !modTalkBNDNames.Any(x.Contains)))
            {
                BNDViewModels.Add(new BNDViewModel(bndPath, gameInfo));
            }
        }
        else
        {
            ShowErrorMessageBox(
                @"The 'script\talk' folder could not be found in the game directory. Make sure your game is unpacked using UXM and then relaunch ESD Studio.");
        }

        ICollectionView collectionView = CollectionViewSource.GetDefaultView(BNDViewModels);
        collectionView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
    }

    private void ShowBNDControl()
    {
        PopulateBNDViewModels(Project.Current.ModDirectory, Project.Current.GameDirectory, Project.Current.Game);
        ((RelayCommand)NewBNDCommand).NotifyCanExecuteChanged();
    }

    private void NewBND()
    {
        List<string> BNDNames = BNDViewModels.Select(x => x.Name).ToList();
        MainWindowNewBNDViewModel newBndViewModel = new(Project.Current.Game, BNDNames);
        MainWindowNewBNDView newBNDView = new()
        {
            Owner = Application.Current.MainWindow,
            ShowInTaskbar = false,
            DataContext = newBndViewModel
        };
        newBNDView.ShowDialog();
        if (newBNDView.DialogResult != true) return;
        int mapId = int.Parse(newBndViewModel.MapIdEntry);
        int blockId = int.Parse(newBndViewModel.BlockIdEntry);
        BNDViewModel newBND = new BNDViewModel(mapId, blockId, newBndViewModel.DescriptionEntry, Project.Current.Game);
        NewBNDCommand command = new(newBND, BNDViewModels);
        command.Redo();
        UndoStack.Push(command);
        /*if (newBND.Description.Length > 0 &&
            !Project.Current.Game.MapDescriptions.Keys.Any(x => x.Equals(newBND.Name)))
        {
            Project.Current.Game.MapDescriptions.Add(newBND.Name, newBND.Description);
            newBND.DescriptionEditCount++;
        }

        BNDViewModels.Add(newBND);
        ICollectionView collectionView = CollectionViewSource.GetDefaultView(BNDViewModels);
        collectionView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));*/
    }

    private bool CanMakeNewBND()
    {
        return BNDViewModels.Count > 0;
    }

    public void OpenNewTab(ESDViewModel ESDToOpen)
    {
        if (OpenTabs.Contains(ESDToOpen)) return;
        /*if (ESDToOpen.IsDecompiled == false)
        {
            ESDToOpen.Decompile(Project.Current.ModDirectory, Project.Current.GameDirectory);
        }
        OpenTabs.Add(ESDToOpen);
        CurrentTab = ESDToOpen;*/
        OpenESDCommand command = new(ESDToOpen);
        command.Redo();
        UndoStack.Push(command);
    }

    private void CloseCurrentTab()
    {
        if (CurrentTab == null) return;
        CloseESDCommand command = new(CurrentTab, OpenTabs.Contains(CurrentTab), OpenTabs.IndexOf(CurrentTab));
        command.Redo();
        UndoStack.Push(command);
    }
    
    public void CloseTab(ESDViewModel tab)
    {
        if (OpenTabs.Contains(tab) == false) return;
        int tabIndex = OpenTabs.IndexOf(tab);
        OpenTabs.Remove(tab);
        if (CurrentTab == tab)
        {
            if (OpenTabs.Count > tabIndex)
            {
                CurrentTab = OpenTabs[tabIndex];
            }
            else if (OpenTabs.Count > 0)
            {
                CurrentTab = OpenTabs[tabIndex - 1];
            }
            else
            {
                CurrentTab = null;
            }
        }
    }

    private bool CanCloseTab()
    {
        return CurrentTab != null;
    }

    private void Find()
    {
        MainWindowFindViewModel findViewModel = new();
        MainWindowFindView findView = new()
        {
            Owner = Application.Current.MainWindow,
            DataContext = findViewModel
        };
        findView.Show();
    }
    
    private void Replace()
    {
        MainWindowReplaceViewModel replaceViewModel = new();
        MainWindowReplaceView replaceView = new()
        {
            Owner = Application.Current.MainWindow,
            DataContext = replaceViewModel
            
        };
        replaceView.Show();
    }

    private void Save()
    {
        if (CurrentTab == null) return;
        if (CurrentTab.IsESDEdited || CurrentTab.IsDescriptionEdited)
        {
            CurrentTab.SaveCommand.Execute(null);
        }
    }
    
    private void SaveAll()
    {
        foreach (BNDViewModel bnd in BNDViewModels.Where(x => x.IsBNDEdited || x.IsDescriptionEdited))
        {
            bnd.SaveCommand.Execute(null);
        }
    }

    private void Undo()
    {
        if (UndoStack.Count == 0) return;
        CommandBase command = UndoStack.Pop();
        command.Undo();
        RedoStack.Push(command);
    }

    private void Redo()
    {
        if (RedoStack.Count == 0) return;
        CommandBase command = RedoStack.Pop();
        command.Redo();
        UndoStack.Push(command);
    }
    
    private void ShowAbout()
    {
        MainWindowAboutView aboutView = new()
        {
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Owner = Application.Current.MainWindow
        };
        aboutView.ShowDialog();
    }
    
    private void ShowEditorSettings()
    {
        MainWindowEditorSettingsViewModel editorSettingsViewModel = new();
        MainWindowEditorSettingsView editorSettingsView = new()
        {
            ShowInTaskbar = false,
            Owner = Application.Current.MainWindow,
            DataContext = editorSettingsViewModel
        };
        editorSettingsView.ShowDialog();
        if (editorSettingsView.DialogResult != true) return;
        WriteEditorSettings();
    }

    private void LoadEditorSettings()
    {
        string cwd = AppDomain.CurrentDomain.BaseDirectory;
        string tomlPath = cwd + "EditorSettings.toml";
        if (File.Exists(tomlPath))
        {
            TomlTable tomlModel = Toml.ToModel(File.ReadAllText(tomlPath), tomlPath);
            tomlModel.TryGetValue("settings", out object obj);
            TomlTable settings = (TomlTable) obj;
            settings.TryGetValue("useGameDataFlags", out obj);
            Editor.UseGameDataFlags = bool.Parse((string) obj);
        }
        else
        {
            Editor.UseGameDataFlags = false;
            TomlTable model = new() { { "title", "EditorSettings" } };
            TomlTable settings = new()
            {
                { "useGameDataFlags", Editor.UseGameDataFlags.ToString() },
            };
            model.Add("settings", settings);
            File.WriteAllText(cwd + @"\EditorSettings.toml", Toml.FromModel(model));
        }
    }
    
    private void WriteEditorSettings()
    {
        string cwd = AppDomain.CurrentDomain.BaseDirectory;
        TomlTable model = new() { { "title", "EditorSettings" } };
        TomlTable settings = new()
        {
            { "useGameDataFlags", Editor.UseGameDataFlags.ToString() },
        };
        model.Add("settings", settings);
        
        File.WriteAllText(cwd + @"\EditorSettings.toml", Toml.FromModel(model));
    }
}