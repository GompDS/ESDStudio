using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using ESDStudio.Models;
using ESDStudio.Views;
using Tomlyn;
using Tomlyn.Model;

namespace ESDStudio.ViewModels;

public class ESDGroupViewModel : ViewModelBase
{
    public ESDGroupViewModel(string name)
    {
        Model = new ESDGroupModel(name);
        EditCommand = new RelayCommand(Edit);
        AddESDCommand = new RelayCommand<int>(AddESD);
        SaveCommand = new RelayCommand(Save);
    }
    
    public ICommand EditCommand { get; }
    public ICommand AddESDCommand { get; }
    public ICommand SaveCommand { get; }
    public ESDGroupModel Model { get; }

    public string Name
    {
        get
        {
            return Model.Name;
        }
        set
        {
            Model.Name = value;
            OnPropertyChanged();
        }
    }
    
    private ObservableCollection<ESDViewModel> _members = new();
    public ObservableCollection<ESDViewModel> Members
    {
        get
        {
            return _members;
        }
        set
        {
            _members = value;
            OnPropertyChanged();
        }
    }

    private void Edit()
    {
        ESDGroupEditViewModel esdGroupEditViewModel = new ESDGroupEditViewModel(this);
        ESDGroupEditView esdGroupEditView = new()
        {
            Owner = Application.Current.MainWindow,
            ShowInTaskbar = false,
            DataContext = esdGroupEditViewModel
        };
        esdGroupEditView.Show();
        if (esdGroupEditView.DialogResult != true) return;
    }

    private void AddESD(int id)
    {
        object? mainWindow = Application.Current.MainWindow;
        if (mainWindow == null) return;
        MainWindowViewModel mainViewModel = (MainWindowViewModel)((MainWindow)mainWindow).DataContext;
        foreach (ESDViewModel esdViewModel in 
                 mainViewModel.BNDViewModels.SelectMany(x => x.ESDViewModels.Where(y => y.Id == id)))
        {
            if (!Members.Contains(esdViewModel))
            {
                Members.Add(esdViewModel);
                if (esdViewModel.ESDGroup != null)
                {
                    esdViewModel.ESDGroup.Members.Remove(esdViewModel);
                }
                esdViewModel.ESDGroup = this;
            }
        }
    }

    private void Save()
    {
        if (!Directory.Exists(Project.Current.BaseDirectory + @"\Groups"))
        {
            Directory.CreateDirectory(Project.Current.BaseDirectory + @"\Groups");
        }
        TomlTable model = new() { { "title", "ESDGroup" } };
        TomlTable groupInfo = new TomlTable()
        {
            { "name", Name },
            { "members", Members.Select(x => x.Id).ToArray() }
        };
        model.Add("group_info", groupInfo);
        File.WriteAllText(Project.Current.BaseDirectory + $"\\Groups\\{Name}.toml", Toml.FromModel(model));
    }

    public void SaveMembers(string codeTemplate, string templateName)
    {
        foreach (ESDViewModel esdViewModel in Members)
        {
            string correctedCode = codeTemplate.Replace(templateName,
                "t" + esdViewModel.Id.ToString($"D{Project.Current.Game.IdLength}"));
            esdViewModel.Code.Text = correctedCode;
            esdViewModel.SaveESD();
        }
    }
}