using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using ESDStudio.Commands;
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
        TextEditor? editor = GetActiveESD()?.CodeEditor;
        if (editor == null) return;
        FindMatch(editor, editor.CaretOffset + editor.TextArea.Selection.Length, true, out Match m, out int nextIndex);
        if (m.Success == false)
        {
            ShowErrorMessageBox($"Can't find the text: \"{FindEntry}\".");
            return;
        }
        editor.Document.BeginUpdate();
        editor.CaretOffset = nextIndex - _findEntry.Length;
        editor.TextArea.Caret.BringCaretToView();
        TextViewPosition pos = new(editor.TextArea.Caret.Line, editor.TextArea.Caret.Column + m.Value.Length);
        editor.TextArea.Selection = new RectangleSelection(editor.TextArea, editor.TextArea.Caret.Position, pos);
        editor.Document.EndUpdate();
    }

    private void FindMatch(TextEditor editor, int startIndex, bool isWrapWround, out Match match, out int nextIndex)
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
            nextIndex = match.Index + startIndex + match.Length;

            if (match.Success == false && isWrapWround)
            {
                match = Regex.Match(editor.Text, pattern, options);
                nextIndex = match.Index;
            }
        }
    }

    private ESDView? GetActiveESD()
    {
        if (Application.Current.MainWindow is MainWindow mainWindow)
        {
            TabItem tabItem = (TabItem)mainWindow.Tabs.ItemContainerGenerator
                .ContainerFromItem(mainWindow.Tabs.SelectedItem);
            if (tabItem.Content is ESDView codeTextBox)
            {
                return codeTextBox;
            }
        }

        return null;
    }

    private void Replace()
    {
        ESDView? esd = GetActiveESD();
        if (esd == null) return;
        TextEditor? editor = esd.CodeEditor;
        if (editor == null || FindEntry.Length == 0) return;
        Selection selected = editor.TextArea.Selection;
        TextViewPosition startPosition;
        if (selected.IsEmpty)
        {
            startPosition = editor.TextArea.Caret.Position;
        }
        else
        {
            startPosition = selected.EndPosition;
        }
        
        DocumentLine startLine = editor.Document.GetLineByNumber(startPosition.Line);
        int startOffset = startLine.Offset + startPosition.VisualColumn;
        FindMatch(editor, startOffset, false, out Match match, out int nextIndex);
        if (match.Success == false)
        {
            ShowErrorMessageBox($"Could not find '{FindEntry}'.");
            return;
        }

        int matchOffset = match.Index + startOffset;
        esd.CodeEditor.Document.BeginUpdate();
        esd.CodeEditor.Document.Replace(matchOffset, FindEntry.Length, ReplaceEntry);
        esd.CodeEditor.CaretOffset = matchOffset;
        esd.CodeEditor.TextArea.Caret.BringCaretToView();
        Caret caret = esd.CodeEditor.TextArea.Caret;
        TextViewPosition startPos = new(caret.Line, caret.Column, caret.VisualColumn);
        TextViewPosition endPos = new(caret.Line, caret.Column + ReplaceEntry.Length, caret.VisualColumn + ReplaceEntry.Length);
        esd.CodeEditor.TextArea.Selection = new RectangleSelection(esd.CodeEditor.TextArea, startPos, endPos);
        esd.CodeEditor.Document.EndUpdate();
    }
    
    private void ReplaceAll()
    {
        ESDView? esd = GetActiveESD();
        if (esd == null) return;
        string pattern = FindEntry;
        if (!IsRegex) pattern = Regex.Escape(pattern);
        if (IsMatchWholeWord) pattern = $"\\b{pattern}\\b";
        RegexOptions options = IsMatchCase ? RegexOptions.None : RegexOptions.IgnoreCase;

        MatchCollection matches = Regex.Matches(esd.CodeEditor.Text, pattern, options);
        //List<Match> matches = new();
        //FindMatch(esd.CodeEditor, 0, false, out Match m, out int nextIndex);
        //int replaceCount = 0;
        /*while (m.Success)
        {
            matches.Add(m);
            /*editor.Text = editor.Text.Remove(nextIndex, m.Value.Length);
            editor.Text = editor.Text.Insert(nextIndex, ReplaceEntry);
            editor.CaretOffset = nextIndex;
            replaceCount++;*/
            //FindMatch(esd.CodeEditor, nextIndex, false, out m, out nextIndex);
        //}
        
        if (matches.Count == 0)
        {
            ShowErrorMessageBox($"Can't find the text: \"{FindEntry}\".");
            return;
        }
        
        esd.CodeEditor.Document.BeginUpdate();
        for (int i = 0; i < matches.Count; i++)
        {
            esd.CodeEditor.Document.Replace(matches[i].Index + ((ReplaceEntry.Length - FindEntry.Length) * i),
                FindEntry.Length, ReplaceEntry);
        }
        esd.CodeEditor.Document.EndUpdate();
        
        MessageBox.Show($"Replaced {matches.Count} occurrences of '{FindEntry}' with '{ReplaceEntry}'.",
            "Replace All", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}