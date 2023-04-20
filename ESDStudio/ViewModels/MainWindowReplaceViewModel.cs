using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using ESDStudio.UserControls;
using ESDStudio.Views;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace ESDStudio.ViewModels;

public class MainWindowReplaceViewModel : ViewModelBase
{
    public MainWindowReplaceViewModel()
    {
        FindNextCommand = new RelayCommand(FindNext);
        ReplaceCommand = new RelayCommand(Replace);
        ReplaceAllCommand = new RelayCommand(ReplaceAll);
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
    
    private string _replaceEntry = "";
    public string ReplaceEntry
    {
        get => _replaceEntry;
        set
        {
            if (_replaceEntry != value)
            {
                _replaceEntry = value;
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
    public ICommand ReplaceCommand { get; }
    public ICommand ReplaceAllCommand { get; }

    private void FindNext()
    {
        TextEditor? editor = GetTextEditor();
        if (editor == null) return;
        FindMatch(editor, editor.CaretOffset + editor.TextArea.Selection.Length, out Match m, out int nextIndex);
        if (m.Success == false)
        {
            ShowErrorMessageBox($"Can't find the text: \"{FindEntry}\".");
            return;
        };
        editor.CaretOffset = nextIndex;
        editor.TextArea.Caret.BringCaretToView();
        TextViewPosition pos = new(editor.TextArea.Caret.Line, editor.TextArea.Caret.Column + m.Value.Length);
        editor.TextArea.Selection = new RectangleSelection(editor.TextArea, editor.TextArea.Caret.Position, pos);
    }

    private void FindMatch(TextEditor editor, int startIndex, out Match match, out int nextIndex)
    {
        match = Match.Empty;
        nextIndex = 0;
        if (FindEntry.Length > 0)
        {
            string pattern = FindEntry;
            if (!IsRegex) pattern = Regex.Escape(pattern);
            if (IsMatchWholeWord) pattern = $"\\b{pattern}\\b";
            RegexOptions options = IsMatchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
            try
            {
                match = Regex.Match(editor.Text[startIndex..], pattern, options);
            }
            catch (Exception e)
            {
                ShowErrorMessageBox(e.Message);
                return;
            }
            nextIndex = match.Index + startIndex;

            if (match.Success == false)
            {
                match = Regex.Match(editor.Text, pattern, options);
                nextIndex = match.Index;
            }
        }
    }

    private TextEditor? GetTextEditor()
    {
        if (Application.Current.MainWindow is MainWindow mainWindow)
        {
            TabItem tabItem = (TabItem)mainWindow.Tabs.ItemContainerGenerator
                .ContainerFromItem(mainWindow.Tabs.SelectedItem);
            if (tabItem.Content is ESDView codeTextBox)
            {
                return codeTextBox.CodeEditor;
            }
        }

        return null;
    }

    private void Replace()
    {
        TextEditor? editor = GetTextEditor();
        if (editor == null || FindEntry.Length == 0) return;
        Selection selected = editor.TextArea.Selection;
        string selectedText = selected.GetText();
        string pattern = FindEntry;
        if (!IsRegex) pattern = Regex.Escape(pattern);
        if (IsMatchWholeWord) pattern = $"\\b{pattern}\\b";
        RegexOptions options = IsMatchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
        Match match;
        try
        {
            match = Regex.Match(selectedText, pattern, options);
        }
        catch (Exception e)
        {
            ShowErrorMessageBox(e.Message);
            return;
        }

        if (match.Success == false)
        {
            ShowErrorMessageBox($"Selected text does not match '{FindEntry}'.");
            return;
        }
        DocumentLine line = editor.Document.GetLineByNumber(selected.StartPosition.Line);
        int replaceOffset = line.Offset + selected.StartPosition.VisualColumn;
        editor.Text = editor.Text.Remove(replaceOffset, selectedText.Length);
        editor.Text = editor.Text.Insert(replaceOffset, ReplaceEntry);
        editor.CaretOffset = replaceOffset;
        editor.TextArea.Caret.BringCaretToView();
        Caret caret = editor.TextArea.Caret;
        TextViewPosition startPos = new(caret.Line, caret.Column, caret.VisualColumn);
        TextViewPosition endPos = new(caret.Line, caret.Column + ReplaceEntry.Length, caret.VisualColumn + ReplaceEntry.Length);
        editor.TextArea.Selection = new RectangleSelection(editor.TextArea, startPos, endPos);
    }
    
    private void ReplaceAll()
    {
        TextEditor? editor = GetTextEditor();
        if (editor == null) return;
        FindMatch(editor, 0, out Match m, out int nextIndex);
        int replaceCount = 0;
        while (m.Success)
        {
            editor.Text = editor.Text.Remove(nextIndex, m.Value.Length);
            editor.Text = editor.Text.Insert(nextIndex, ReplaceEntry);
            editor.CaretOffset = nextIndex;
            replaceCount++;
            FindMatch(editor, nextIndex, out m, out nextIndex);
        }

        if (replaceCount > 0)
        {
            MessageBox.Show($"Replaced {replaceCount} occurrences of '{FindEntry}' with '{ReplaceEntry}'.",
                "Replace All", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            ShowErrorMessageBox($"Can't find the text: \"{FindEntry}\".");
        }
    }
}