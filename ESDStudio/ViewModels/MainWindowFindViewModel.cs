using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using ESDStudio.UserControls;
using ESDStudio.Views;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;

namespace ESDStudio.ViewModels;

public class MainWindowFindViewModel : ViewModelBase
{
    public MainWindowFindViewModel()
    {
        FindNextCommand = new RelayCommand(FindNext);
    }

    private string _findEntry = "";
    public string FindEntry
    {
        get => _findEntry;
        set
        {
            if (_findEntry != value)
            {
                _findEntry = value;
                OnPropertyChanged();
            }
        }
    }
    
    private bool _isMatchCase;
    public bool IsMatchCase
    {
        get => _isMatchCase;
        set
        {
            if (_isMatchCase != value)
            {
                _isMatchCase = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isMatchWholeWord;
    public bool IsMatchWholeWord
    {
        get => _isMatchWholeWord;
        set
        {
            if (_isMatchWholeWord != value)
            {
                _isMatchWholeWord = value;
                OnPropertyChanged();
            }
        }
    }
    
    private bool _isRegex;
    public bool IsRegex
    {
        get => _isRegex;
        set
        {
            if (_isRegex != value)
            {
                _isRegex = value;
                OnPropertyChanged();
            }
        }
    }
    
    public ICommand FindNextCommand { get; }

    private void FindNext()
    {
        if (Application.Current.MainWindow is not MainWindow mainWindow) return;
        TabItem tabItem = (TabItem)mainWindow.Tabs.ItemContainerGenerator.ContainerFromItem(mainWindow.Tabs.SelectedItem);
        if (tabItem.Content is not CodeTextBox codeTextBox) return;
        TextEditor editor = codeTextBox.textEditor;
        if (FindEntry.Length == 0) return;
        string pattern = FindEntry;
        if (!IsRegex) pattern = Regex.Escape(pattern);
        if (IsMatchWholeWord) pattern = $"\\b{pattern}\\b";
        RegexOptions options = IsMatchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
        int startIndex = editor.CaretOffset + editor.TextArea.Selection.Length;
        Match m;
        try
        {
            m = Regex.Match(editor.Text[startIndex..], pattern, options);
        }
        catch (Exception e)
        {
            ShowErrorMessageBox(e.Message);
            return;
        }
        int nextIndex = m.Index + startIndex;

        if (m.Success == false)
        {
            m = Regex.Match(editor.Text, pattern, options);
            nextIndex = m.Index;
        }
        
        if (m.Success == false)
        {
            ShowErrorMessageBox($"Can't find the text: \"{FindEntry}\".");
            return;
        }
        editor.CaretOffset = nextIndex;
        editor.TextArea.Caret.BringCaretToView();
        TextViewPosition pos = new(editor.TextArea.Caret.Line, editor.TextArea.Caret.Column + m.Value.Length);
        editor.TextArea.Selection = new RectangleSelection(editor.TextArea, editor.TextArea.Caret.Position, pos);
    }
}