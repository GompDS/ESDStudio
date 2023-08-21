using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace ESDStudio.ViewModels;

public class MainWindowEditorSettingsViewModel : DialogViewModelBase
{
    public MainWindowEditorSettingsViewModel()
    {
        _gameDataFlagsCheck = Editor.UseGameDataFlags;
    }
    
    private bool _gameDataFlagsCheck;
    public bool GameDataFlagsCheck
    {
        get => _gameDataFlagsCheck;
        set
        {
            if (_gameDataFlagsCheck != value)
            {
                _gameDataFlagsCheck = value;
                OnPropertyChanged();
            }
        }
    }

    protected override void Confirm()
    {
        Editor.UseGameDataFlags = GameDataFlagsCheck;
        DialogResult = true;
    }

}