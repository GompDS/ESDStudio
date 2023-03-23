using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using ESDStudio.Views;
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
        OpenProjectCommand = new RelayCommand(OpenProject);
        NewProjectCommand = new RelayCommand(NewProject);
    }

    private BNDViewModel _BNDControl = new();
    public BNDViewModel BNDControl
    {
        get
        {
            return _BNDControl;
        }
        set
        {
            if (_BNDControl != value)
            {
                _BNDControl = value;
                OnPropertyChanged();
            }
        }
    }
    public ICommand OpenProjectCommand { get; }
    public ICommand NewProjectCommand { get; }

    private string _projectName = "";
    public string ProjectName
    {
        get
        {
            return _projectName;
        }
        set
        {
            if (_projectName != value)
            {
                _projectName = value;
                OnPropertyChanged();
            }
        }
    }
    
    private string _projectBaseDirectory = "";
    public string ProjectBaseDirectory
    {
        get
        {
            return _projectBaseDirectory;
        }
        set
        {
            if (_projectBaseDirectory != value)
            {
                _projectBaseDirectory = value;
                OnPropertyChanged();
            }
        }
    }
    
    private string _projectGameDirectory = "";
    public string ProjectGameDirectory
    {
        get
        {
            return _projectGameDirectory;
        }
        set
        {
            if (_projectGameDirectory != value)
            {
                _projectGameDirectory = value;
                OnPropertyChanged();
            }
        }
    }
    
    private string _projectModDirectory = "";
    public string ProjectModDirectory
    {
        get
        {
            return _projectModDirectory;
        }
        set
        {
            if (_projectModDirectory != value)
            {
                _projectModDirectory = value;
                OnPropertyChanged();
            }
        }
    }
    
    public GameInfo? ProjectGame { get; set; }

    private void OpenProject()
    {
        OpenFileDialog fileDialog = new()
        {
            Filter = "TOML files (*.toml)|*.toml",
            InitialDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}Projects"
        };
        if (fileDialog.ShowDialog() == true)
        {
            TomlTable model = Toml.ToModel(File.ReadAllText(fileDialog.FileName), fileDialog.FileName);
            model.TryGetValue("project_info", out object obj);
            TomlTable projectInfo = (TomlTable)obj;
            projectInfo.TryGetValue("name", out object nameObj);
            ProjectName = (string)nameObj;
            projectInfo.TryGetValue("base_directory", out object baseDirObj);
            ProjectBaseDirectory = (string)baseDirObj;
            projectInfo.TryGetValue("game_directory", out object gameDirObj);
            ProjectGameDirectory = (string)gameDirObj;
            projectInfo.TryGetValue("mod_directory", out object modDirObj);
            ProjectModDirectory = (string)modDirObj;
            projectInfo.TryGetValue("game", out object gameObj);
            ProjectGame = new GameInfo((string)gameObj);
            ShowBNDControl();
        }
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
        ProjectName = newProjectViewModel.ProjectNameEntry;
        ProjectBaseDirectory = newProjectViewModel.ProjectBaseDirectoryEntry + $"\\{ProjectName}";
        Directory.CreateDirectory(ProjectBaseDirectory);
        string? gameDir = Path.GetDirectoryName(newProjectViewModel.GameExecutableEntry);
        if (gameDir == null) return;
        ProjectGameDirectory = gameDir;
        ProjectModDirectory = newProjectViewModel.ProjectModDirectoryEntry;
        ProjectGame = new GameInfo(newProjectViewModel.GameExecutableEntry);
        
        TomlTable model = new() { { "title", "ESDStudioProject" } };
        TomlTable projectInfo = new()
        {
            { "name", ProjectName },
            { "base_directory", ProjectBaseDirectory },
            { "game_directory", ProjectGameDirectory },
            { "mod_directory", ProjectModDirectory },
            { "game", ProjectGame }
        };
        model.Add("project_info", projectInfo);
        File.WriteAllText(ProjectBaseDirectory + @"\ESDStudioProject.toml", Toml.FromModel(model));
        ShowBNDControl();
    }

    private void ShowBNDControl()
    {
        if (ProjectGame == null) return;
        BNDControl.PopulateBNDModels(ProjectModDirectory, ProjectGameDirectory, ProjectGame);
        BNDControl.Visibility = Visibility.Visible;
    }
}