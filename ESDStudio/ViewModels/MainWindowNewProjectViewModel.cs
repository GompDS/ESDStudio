using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Ookii.Dialogs.Wpf;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace ESDStudio.ViewModels;

public class MainWindowNewProjectViewModel : ViewModelBase
{
    public MainWindowNewProjectViewModel()
    {
        ConfirmCommand = new RelayCommand(Confirm);
        CancelCommand = new RelayCommand(Cancel);
        BrowseBaseDirectoryCommand = new RelayCommand(BrowseForBaseDirectory);
        BrowseExeCommand = new RelayCommand(BrowseForExe);
        BrowseModDirectoryCommand = new RelayCommand(BrowseForModDirectory);
        if (Directory.Exists(ProjectBaseDirectoryEntry) == false)
        {
            Directory.CreateDirectory(ProjectBaseDirectoryEntry);
        }
    }
    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand BrowseBaseDirectoryCommand { get; }
    public ICommand BrowseExeCommand { get; }
    public ICommand BrowseModDirectoryCommand { get; }

    private bool? _dialogResult;
    public bool? DialogResult
    {
        get => _dialogResult;
        set
        {
            if (_dialogResult != value)
            {
                _dialogResult = value;
                OnPropertyChanged();
            }
        }
    }

    private string _projectNameEntry = "MyProject";
    public string ProjectNameEntry
    {
        get => _projectNameEntry;
        set
        {
            if (_projectNameEntry != value)
            {
                _projectNameEntry = value;
                OnPropertyChanged();
            }
        }
    }
    
    private string _projectBaseDirectoryEntry = AppDomain.CurrentDomain.BaseDirectory + @"Projects";
    public string ProjectBaseDirectoryEntry
    {
        get => _projectBaseDirectoryEntry;
        set
        {
            if (_projectBaseDirectoryEntry != value)
            {
                _projectBaseDirectoryEntry = value;
                OnPropertyChanged();
            }
        }
    }
    
    private string _gameExecutableEntry = "";
    public string GameExecutableEntry
    {
        get =>  _gameExecutableEntry;
        set
        {
            if ( _gameExecutableEntry != value)
            {
                _gameExecutableEntry = value;
                OnPropertyChanged();
            }
        }
    }
    
    private string _projectModDirectoryEntry = "";
    public string ProjectModDirectoryEntry
    {
        get => _projectModDirectoryEntry;
        set
        {
            if (_projectModDirectoryEntry != value)
            {
                _projectModDirectoryEntry = value;
                OnPropertyChanged();
            }
        }
    }
    
    private void Confirm()
    {
        ProjectNameEntry = ProjectNameEntry.Replace(" ", "");
        if (ProjectBaseDirectoryEntry.EndsWith("\\")) ProjectBaseDirectoryEntry = ProjectBaseDirectoryEntry[..^1];
        if (ProjectModDirectoryEntry.EndsWith("\\")) ProjectModDirectoryEntry = ProjectModDirectoryEntry[..^1];
        
        if (ProjectNameEntry.Length == 0)
        {
            MessageBoxResult result = ShowErrorMessageBox("Project Name must be at least 1 character long.");
            if (result == MessageBoxResult.OK) return;
        }
        
        if (Directory.Exists(ProjectBaseDirectoryEntry) == false)
        {
            MessageBoxResult result = ShowErrorMessageBox("Project Base Directory does not exist.");
            if (result == MessageBoxResult.OK) return;
        }

        if (Directory.Exists(ProjectBaseDirectoryEntry + $"\\{ProjectNameEntry}"))
        {
            MessageBoxResult result = ShowErrorMessageBox("A project with that name already exists.");
            if (result == MessageBoxResult.OK) return;
        }
        
        if (File.Exists(GameExecutableEntry) == false)
        {
            MessageBoxResult result = ShowErrorMessageBox("Game Executable does not exist.");
            if (result == MessageBoxResult.OK) return;
        }

        if (!GameInfo.IsValidExecutable(GameExecutableEntry))
        {
            MessageBoxResult result = ShowErrorMessageBox("Executable is from an unsupported game.");
            if (result == MessageBoxResult.OK) return;
        }
        
        if (Directory.Exists(ProjectModDirectoryEntry) == false)
        {
            MessageBoxResult result = ShowErrorMessageBox("Project Mod Directory does not exist.");
            if (result == MessageBoxResult.OK) return;
        }
        DialogResult = true;
    }

    private void Cancel()
    {
        DialogResult = false;
    }

    private void BrowseForBaseDirectory()
    {
        VistaFolderBrowserDialog folderBrowserDialog = new()
        {
            Description = "Select Project Base Directory",
            UseDescriptionForTitle = true,
            SelectedPath = ProjectBaseDirectoryEntry
        };
        if (folderBrowserDialog.ShowDialog() == true)
        {
            ProjectBaseDirectoryEntry = folderBrowserDialog.SelectedPath;
        }
    }
    
    private void BrowseForExe()
    {
        OpenFileDialog fileDialog = new()
        {
            Title = "Select Game Executable",
            Filter = "Executable files (*.exe)|*.exe",
            InitialDirectory = Path.GetDirectoryName(GameExecutableEntry)
        };
        if (fileDialog.ShowDialog() == true)
        {
            GameExecutableEntry = fileDialog.FileName;
        }
    }
    
    private void BrowseForModDirectory()
    {
        VistaFolderBrowserDialog folderBrowserDialog = new()
        {
            Description = "Select Project Mod Directory",
            UseDescriptionForTitle = true,
            SelectedPath = ProjectModDirectoryEntry
        };
        if (folderBrowserDialog.ShowDialog() == true)
        {
            ProjectModDirectoryEntry = folderBrowserDialog.SelectedPath;
        }
    }
}