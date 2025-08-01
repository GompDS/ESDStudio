﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ESDLang.Doc;
using ESDStudio.UserControls;
using ESDStudio.ViewModels;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace ESDStudio.Views;

public partial class ESDView : UserControl
{
    public ESDView(ESDViewModel dataContext, DetailPanel detailPanel)
    {
        InitializeComponent();
        DataContext = dataContext;
        ToolTip editorToolTip = new ToolTip
        {
            IsOpen = false
        };
        ToolTipService.SetPlacementTarget(editorToolTip, CodeEditor);
        ToolTipService.SetPlacement(editorToolTip, PlacementMode.Top);
        //ToolTipService.SetVerticalOffset(editorToolTip, -5.0);
        CodeEditor.ToolTip = editorToolTip;
        _detailedFunctionView = detailPanel;
        CodeEditor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
        CodeEditor.TextArea.TextEntered += textEditor_TextArea_TextEntered;
        CodeEditor.TextArea.Caret.PositionChanged += TextEditor_OnCaretPositionChanged;
        CodeEditor.IsEnabled = true;
        completionTimer.Elapsed += CompletionTimerOnElapsed;
    }

    private void CompletionTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            completionTimer.Stop();
            DocumentLine line = CodeEditor.Document.GetLineByNumber(CodeEditor.TextArea.Caret.Line);
            string enteredText = CodeEditor.Text.Substring(line.Offset, CodeEditor.TextArea.Caret.Column - 1);
            int insertionOffset = line.Offset;
            _completionWindow = new CompletionWindow(CodeEditor.TextArea)
            {
                Style = Application.Current.FindResource("CodeCompletionWindow") as Style,
                SizeToContent = SizeToContent.WidthAndHeight
            };
            IList<ICompletionData> data = _completionWindow.CompletionList.CompletionData;
            int searchStartIndex = line.Offset + enteredText.Length - 1;
            string searchRange = CodeEditor.Text.Substring(0, searchStartIndex + 1);
            CodeEditorUtils.GetParentFunctionInfo(searchStartIndex, searchRange, out string parentFunctionName, out int parameterIndex);
            
            int delimIndex = -1;
            for (int i = enteredText.Length - 1; i >= 0; i--)
            {
                if (CodeEditorUtils.Delimiters.Match(enteredText[i].ToString()).Success)
                {
                    delimIndex = i;
                    break;
                }
            }
            if (delimIndex > -1)
            {
                enteredText = enteredText.Substring(delimIndex + 1);
                insertionOffset = line.Offset + delimIndex + 1;
            }

            if (enteredText.Length == 0) return;

            foreach (CompletionData completionTerm in CodeEditorUtils.CompletionTerms.Where(x =>
                         Regex.IsMatch(x.Text, Regex.Escape(enteredText), RegexOptions.IgnoreCase)))
            {
                //if (!completionTerm.Text.StartsWith(enteredText, StringComparison.OrdinalIgnoreCase)) continue;
                //if (!Regex.IsMatch(completionTerm, Regex.Escape(enteredText), RegexOptions.IgnoreCase)) continue;
                if (completionTerm.Text.StartsWith(enteredText, StringComparison.OrdinalIgnoreCase))
                {
                    completionTerm.Priority = 2.0;
                }
                else
                {
                    completionTerm.Priority = 1.0;
                }
                completionTerm.InsertionOffset = insertionOffset;
                completionTerm.LengthToRemove = enteredText.Length;
                completionTerm.ParameterIndex = parameterIndex;
                data.Add(completionTerm);
            }

            for (int p = 0; p <= data.Count - 2; p++)
            {
                for (int i = 0; i <= data.Count - 2; i++)
                {
                    if (data[i].Priority < data[i + 1].Priority)
                    {
                        ICompletionData t = data[i + 1];
                        data[i + 1] = data[i];
                        data[i] = t;
                    }
                } 
            }
            if (data.Count == 0) return;
            _completionWindow.Show();
            _completionWindow.Closed += delegate {
                _completionWindow = null;
            };
        });
    }

    private bool _isReadyToEdit = false;
    private DetailPanel _detailedFunctionView;
    private CompletionWindow? _completionWindow;

    private enum ToolTipMode
    {
        Function,
        Parameters
    }

    private ToolTipMode _toolTipMode;
    private int _lastFocusedTermStartIndex = -1;
    private Timer completionTimer = new(0.5 * 1000);

    private void textEditor_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
    {
        completionTimer.Stop();
        completionTimer.Start();
    }

    private void textEditor_TextArea_TextEntering(object sender, TextCompositionEventArgs e)
    {
        if (e.Text.Length > 0 && _completionWindow != null) 
        {
            _completionWindow.CompletionList.RequestInsertion(e);
        }
    }

    private void TextEditor_OnMouseHover(object sender, MouseEventArgs e)
    {
        TextViewPosition? position = CodeEditor.GetPositionFromPoint(Mouse.GetPosition(CodeEditor));
        if (position == null) return;
        GetOverlappedTerm(position.Value, out string selectedTerm, out int termStartIndex);
        ESDDocumentation.MethodDoc? selectedFunc = Project.Current.Game.TalkMethods
            .FirstOrDefault(x => x.Name != null && x.Name.Equals(selectedTerm, StringComparison.OrdinalIgnoreCase));
        ToolTip funcToolTip = (ToolTip)CodeEditor.ToolTip;
        
        if (selectedFunc != null && _lastFocusedTermStartIndex != termStartIndex)
        {
            _lastFocusedTermStartIndex = termStartIndex;
            funcToolTip.Content = new FunctionToolTip(selectedFunc);
            ToolTipService.SetPlacementRectangle(funcToolTip,
                new Rect(Mouse.GetPosition(CodeEditor), new Size(CodeEditor.FontSize, CodeEditor.FontSize)));
            //funcToolTip.PlacementRectangle = new Rect(Mouse.GetPosition(textEditor), new Size(textEditor.FontSize, textEditor.FontSize));
            funcToolTip.IsOpen = true;
            _toolTipMode = ToolTipMode.Function;
        }
    }

    private void TextEditor_OnMouseMove(object sender, MouseEventArgs e)
    {
        TextViewPosition? position = CodeEditor.GetPositionFromPoint(Mouse.GetPosition(CodeEditor));
        if (position == null) return;
        GetOverlappedTerm(position.Value, out string selectedTerm, out int termStartIndex);
        ESDDocumentation.MethodDoc? selectedFunc = Project.Current.Game.TalkMethods
            .FirstOrDefault(x => x.Name != null && x.Name.Equals(selectedTerm, StringComparison.OrdinalIgnoreCase));
        ToolTip funcToolTip = (ToolTip)CodeEditor.ToolTip;

        if (selectedFunc != null && termStartIndex != _lastFocusedTermStartIndex)
        {
            funcToolTip.IsOpen = false;
            _lastFocusedTermStartIndex = -1;
        }
        else
        {
            if (termStartIndex + 1 >= CodeEditor.Text.Length) return;
            string searchRange = CodeEditor.Text[..(termStartIndex + 1)];
            CodeEditorUtils.GetParentFunctionInfo(termStartIndex, searchRange, out string parentFunctionName, out int parameterIndex);
            ESDDocumentation.MethodDoc? parentFunc = Project.Current.Game.TalkMethods
                .FirstOrDefault(x => x.Name != null && x.Name.Equals(parentFunctionName, StringComparison.OrdinalIgnoreCase));
            if (parentFunc == null || _toolTipMode == ToolTipMode.Function)
            {
                funcToolTip.IsOpen = false;
                _lastFocusedTermStartIndex = -1;
            }
        }
    }
    
    private void TextEditor_OnCaretPositionChanged(object? sender, EventArgs e)
    {
        if (sender == null) return;
        Caret caret = (Caret)sender;
        GetOverlappedTerm(caret.Position, out string selectedTerm, out int termStartIndex);

        ESDDocumentation.MethodDoc? selectedFunc = Project.Current.Game.TalkMethods
            .FirstOrDefault(x => x.Name != null && x.Name.Equals(selectedTerm, StringComparison.OrdinalIgnoreCase));

        ToolTip funcToolTip = (ToolTip)CodeEditor.ToolTip;
        if (selectedFunc != null)
        {
            _detailedFunctionView.SetFunctionToDisplay(selectedFunc, -1);
            if (_toolTipMode == ToolTipMode.Parameters)
            {
                funcToolTip.IsOpen = false;
                _lastFocusedTermStartIndex = -1;
            }
        }
        else if (termStartIndex < CodeEditor.Text.Length - 1 && termStartIndex > 0)
        {
            string searchRange = CodeEditor.Text[..(termStartIndex + 1)];
            CodeEditorUtils.GetParentFunctionInfo(termStartIndex, searchRange, out string parentFunctionName, out int parameterIndex);
            ESDDocumentation.MethodDoc? parentFunc = Project.Current.Game.TalkMethods
                .FirstOrDefault(x => x.Name != null && x.Name.Equals(parentFunctionName, StringComparison.OrdinalIgnoreCase));
            if (parentFunc is { Args.Count: > 0 })
            {
                _detailedFunctionView.SetFunctionToDisplay(parentFunc, parameterIndex);
                funcToolTip.Content = new FunctionToolTip(parentFunc, parameterIndex);
                Rect caretRect = caret.CalculateCaretRectangle();
                TranslateTransform transform = new(-CodeEditor.HorizontalOffset, -CodeEditor.VerticalOffset);
                Matrix matrix = transform.Value;
                caretRect.Transform(matrix);
                ToolTipService.SetPlacementRectangle(funcToolTip, caretRect);
                funcToolTip.IsOpen = true;
                _toolTipMode = ToolTipMode.Parameters;
            }
            else
            {
                _detailedFunctionView.ClearDisplay();
                funcToolTip.IsOpen = false;
                _lastFocusedTermStartIndex = -1;
            }
        }
    }

    private void GetOverlappedTerm(TextViewPosition textPosition, out string term, out int termStartIndex)
    {
        term = "";
        DocumentLine line = CodeEditor.Document.GetLineByNumber(textPosition.Line);
        string lineText = CodeEditor.Text.Substring(line.Offset, line.Length);
        termStartIndex = textPosition.VisualColumn;
        if (termStartIndex < line.Length && termStartIndex >= 1)
        {
            while (!CodeEditorUtils.Delimiters.IsMatch(lineText[termStartIndex].ToString()))
            {
                termStartIndex++;
                if (termStartIndex >= lineText.Length) break;
            }

            termStartIndex -= 1;
            while (!CodeEditorUtils.Delimiters.IsMatch(lineText[termStartIndex].ToString()))
            {
                term = term.Insert(0, lineText[termStartIndex].ToString());
                if (termStartIndex <= 0) break;
                termStartIndex--;
            }
        }

        termStartIndex += line.Offset;
    }

    private void TextEditor_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control) return;
        
        if (e.Delta > 0)
        {
            CodeEditor.FontSize += 1.0;
            //textEditor.ScrollToVerticalOffset(textEditor.VerticalOffset + textEditor.FontSize * 2);
        }
        else if (CodeEditor.FontSize > 1.0)
        {
            CodeEditor.FontSize -= 1.0;
            //textEditor.ScrollToVerticalOffset(textEditor.VerticalOffset - textEditor.FontSize * 2);
        }
        
        CodeEditor.TextArea.Caret.BringCaretToView();

        e.Handled = true;
    }

    private void CodeEditor_OnTextChanged(object? sender, EventArgs eventArgs)
    {

        TextEditor? editor = (TextEditor?) sender;
        if (editor == null) return;
        if (editor.Document == null) return;
        
        if (_isReadyToEdit == false)
        {
            _isReadyToEdit = true;
            return;
        }

        ESDViewModel esd = (ESDViewModel)DataContext;
        esd.ESDEditCount++;
    }
}