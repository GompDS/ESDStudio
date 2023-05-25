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
        XmlData.ReadFunctionDefXml();
        RecentProjects = new ObservableCollection<Project>();
        BNDViewModels = new ObservableCollection<BNDViewModel>();
        OpenTabs = new ObservableCollection<ESDViewModel>();
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
        BNDViewModels.Clear();
        OpenTabs.Clear();
        Project.Current = project;
        OnPropertyChanged("ProjectName");
        ShowBNDControl();
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
        Directory.CreateDirectory(baseDir);
        string? gameDir = Path.GetDirectoryName(newProjectVM.GameExecutableEntry);
        if (gameDir == null) return;
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
        if (newBND.Description.Length > 0 &&
            !Project.Current.Game.MapDescriptions.Keys.Any(x => x.Equals(newBND.Name)))
        {
            Project.Current.Game.MapDescriptions.Add(newBND.Name, newBND.Description);
            newBND.DescriptionEditCount++;
        }

        BNDViewModels.Add(newBND);
        ICollectionView collectionView = CollectionViewSource.GetDefaultView(BNDViewModels);
        collectionView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
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
        command.Execute(null);
        UndoStack.Push(command);
    }

    private void CloseCurrentTab()
    {
        if (CurrentTab == null) return;
        CloseESDCommand command = new(CurrentTab, OpenTabs.Contains(CurrentTab), OpenTabs.IndexOf(CurrentTab));
        command.Execute(null);
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
        command.Execute(null);
        UndoStack.Push(command);
    }
}