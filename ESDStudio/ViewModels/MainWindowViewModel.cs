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
using ESDStudio.Models;
using ESDStudio.UserControls;
using ESDStudio.Views;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using Tomlyn;
using Tomlyn.Model;
// ReSharper disable MemberCanBePrivate.Global

namespace ESDStudio.ViewModels;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        BNDViewModels = new ObservableCollection<BNDViewModel>();
        OpenTabs = new ObservableCollection<ESDViewModel>();
        UndoCommand = new RelayCommand(Undo);
        RedoCommand = new RelayCommand(Redo);
        OpenProjectCommand = new RelayCommand(OpenProject);
        NewProjectCommand = new RelayCommand(NewProject);
        NewBNDCommand = new RelayCommand(NewBND, CanSaveBND);
        CloseTabCommand = new RelayCommand(CloseCurrentTab, CanCloseTab);
        FindCommand = new RelayCommand(Find, CanCloseTab);
        ReplaceCommand = new RelayCommand(Replace, CanCloseTab);
        OpenRecentProjectCommand = new RelayCommand(OpenRecentProject);
        SaveCommand = new RelayCommand(Save);
        SaveAllCommand = new RelayCommand(SaveAll);
        RecentProjects = GetRecentProjects();
        if (RecentProjects.Count > 0)
        {
            LoadProjectFromToml(RecentProjects[0].Item2);
        }
        XmlData.ReadFunctionDefXml();
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

    private ObservableCollection<Tuple<string, string>> _recentProjects = new();
    public ObservableCollection<Tuple<string, string>> RecentProjects
    {
        get
        {
            return _recentProjects;
        }
        set
        {
            _recentProjects = value;
            OnPropertyChanged();
        }
    }

    public string ProjectName
    {
        get
        {
            return ProjectData.Name;
        }
        set
        {
            if (ProjectData.Name != value)
            {
                ProjectData.Name = value;
                OnPropertyChanged();
            }
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

    private ObservableCollection<Tuple<string, string>> GetRecentProjects()
    {
        ObservableCollection<Tuple<string, string>> recentProjects = new();
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
                recentProjects.Add(new Tuple<string, string>(projectName, projectPath));
            }
        }

        return recentProjects;
    }

    private void AddCurrentProjectToRecent()
    {
        for (int i = 5; i <= RecentProjects.Count;)
        {
            RecentProjects.Remove(RecentProjects[i-1]);
        }

        if (RecentProjects.Any(x => x.Item1.Equals(ProjectName)))
        {
            RecentProjects.Remove(RecentProjects.First(x => x.Item1.Equals(ProjectName)));
        }
        RecentProjects.Insert(0, 
            new Tuple<string, string>(ProjectName, ProjectData.BaseDirectory + @"\ESDStudioProject.toml"));
        TomlTable model = new TomlTable();
        model.Add("Title", "RecentProjects");
        TomlTable subModel = new TomlTable();
        foreach (Tuple<string,string> project in RecentProjects)
        {
            subModel.Add(project.Item1, project.Item2);
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
            LoadProjectFromToml(fileDialog.FileName);
        }
    }

    private void LoadProjectFromToml(string tomlPath)
    {
        BNDViewModels.Clear();
        OpenTabs.Clear();
        TomlTable model = Toml.ToModel(File.ReadAllText(tomlPath), tomlPath);
        model.TryGetValue("project_info", out object obj);
        TomlTable projectInfo = (TomlTable)obj;
        projectInfo.TryGetValue("name", out object nameObj);
        ProjectName = (string)nameObj;
        projectInfo.TryGetValue("base_directory", out object baseDirObj);
        ProjectData.BaseDirectory = (string)baseDirObj;
        projectInfo.TryGetValue("game_directory", out object gameDirObj);
        ProjectData.GameDirectory = (string)gameDirObj;
        projectInfo.TryGetValue("mod_directory", out object modDirObj);
        ProjectData.ModDirectory = (string)modDirObj;
        projectInfo.TryGetValue("game", out object gameObj);
        ProjectData.Game = new GameInfo((string)gameObj);
        if (File.Exists(ProjectData.BaseDirectory + @"\MapDescriptions.toml"))
        {
            ProjectData.MapDescriptions = ProjectUtils.ReadMapDescriptions(ProjectData.BaseDirectory + @"\MapDescriptions.toml");
        }
        if (File.Exists(ProjectData.BaseDirectory + @"\ESDDescriptions.toml"))
        {
            ProjectData.ESDDescriptions = ProjectUtils.ReadESDDescriptions(ProjectData.BaseDirectory + @"\ESDDescriptions.toml");
        }
        ShowBNDControl();
        AddCurrentProjectToRecent();
    }

    private void NewProject()
    {
        MainWindowNewProjectViewModel newProjectViewModel = new();
        MainWindowNewProjectView newProjectView = new()
        {
            Owner = Application.Current.MainWindow,
            ShowInTaskbar = false,
            DataContext = newProjectViewModel
        };
        newProjectView.ShowDialog();
        if (newProjectView.DialogResult != true) return;
        BNDViewModels.Clear();
        ProjectName = newProjectViewModel.ProjectNameEntry;
        ProjectData.BaseDirectory = newProjectViewModel.ProjectBaseDirectoryEntry + $"\\{ProjectName}";
        Directory.CreateDirectory(ProjectData.BaseDirectory);
        string? gameDir = Path.GetDirectoryName(newProjectViewModel.GameExecutableEntry);
        if (gameDir == null) return;
        ProjectData.GameDirectory = gameDir;
        ProjectData.ModDirectory = newProjectViewModel.ProjectModDirectoryEntry;
        ProjectData.Game = new GameInfo(newProjectViewModel.GameExecutableEntry);
        
        TomlTable model = new() { { "title", "ESDStudioProject" } };
        TomlTable projectInfo = new()
        {
            { "name", ProjectName },
            { "base_directory", ProjectData.BaseDirectory },
            { "game_directory", ProjectData.GameDirectory },
            { "mod_directory", ProjectData.ModDirectory },
            { "game", ProjectData.Game.ToString() }
        };
        model.Add("project_info", projectInfo);
        File.WriteAllText(ProjectData.BaseDirectory + @"\ESDStudioProject.toml", Toml.FromModel(model));
        ShowBNDControl();
        AddCurrentProjectToRecent();
    }

    private void OpenRecentProject()
    {
        if (SelectedRecentProject == null) return;
        LoadProjectFromToml(((Tuple<string, string>)SelectedRecentProject).Item2);
    }
    
    private void PopulateBNDViewModels(string modDirectory, string gameDirectory, GameInfo gameInfo)
    {
        List<string> modTalkBNDNames = new();
        IEnumerable<string> bndPaths = 
            Directory.EnumerateFiles(modDirectory + @"\script\talk", "*.talkesdbnd.dcx");
        foreach (string bndPath in bndPaths)
        {
            string bndName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(bndPath));
            modTalkBNDNames.Add(bndName);
            BNDViewModels.Add(new BNDViewModel(bndPath, gameInfo));
        }
            
        bndPaths = Directory.EnumerateFiles(gameDirectory + @"\script\talk", "*.talkesdbnd.dcx");
        foreach (string bndPath in bndPaths.Where(x => !modTalkBNDNames.Any(x.Contains)))
        {
            BNDViewModels.Add(new BNDViewModel(bndPath, gameInfo));
        }
        
        ICollectionView collectionView = CollectionViewSource.GetDefaultView(BNDViewModels);
        collectionView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
    }

    private void ShowBNDControl()
    {
        if (ProjectData.Game == null) return;
        PopulateBNDViewModels(ProjectData.ModDirectory, ProjectData.GameDirectory, ProjectData.Game);
        ((RelayCommand)NewBNDCommand).NotifyCanExecuteChanged();
    }

    private void NewBND()
    {
        if (ProjectData.Game == null) return;
        List<string> BNDNames = BNDViewModels.Select(x => x.Name).ToList();
        MainWindowNewBNDViewModel newBndViewModel = new(ProjectData.Game, BNDNames);
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
        BNDViewModel newBND = new BNDViewModel(mapId, blockId, newBndViewModel.DescriptionEntry, ProjectData.Game);
        if (newBND.Description.Length > 0 && 
            !ProjectData.Game.MapDescriptions.Keys.Any(x => x.Equals(newBND.Name)))
        {
            ProjectData.Game.MapDescriptions.Add(newBND.Name, newBND.Description);
            newBND.IsDescriptionEdited = true;
        }
        BNDViewModels.Add(newBND);
        ICollectionView collectionView = CollectionViewSource.GetDefaultView(BNDViewModels);
        collectionView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
    }

    private bool CanSaveBND()
    {
        return BNDViewModels.Count > 0;
    }

    public void OpenNewTab(ESDViewModel ESDToOpen)
    {
        if (OpenTabs.Contains(ESDToOpen)) return;
        if (ESDToOpen.IsDecompiled == false)
        {
            ESDToOpen.Decompile(ProjectData.ModDirectory, ProjectData.GameDirectory);
        }
        OpenTabs.Add(ESDToOpen);
        CurrentTab = ESDToOpen;
    }

    private void CloseCurrentTab()
    {
        if (CurrentTab == null) return;
        int tabIndex = OpenTabs.IndexOf(CurrentTab);
        OpenTabs.Remove(CurrentTab);
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
        command.Execute(null);
        UndoStack.Push(command);
    }
}