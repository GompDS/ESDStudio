using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;

namespace ESDStudio.UserControls;

public partial class CodeTextBox : UserControl
{
    public CodeTextBox(string code, DetailPanel detailPanel)
    {
        InitializeComponent();
        textEditor.Text = code;
        textEditor.ToolTip = new ToolTip();
        DetailedFunctionView = detailPanel;
    }

    private DetailPanel DetailedFunctionView;
    private int ScrollLockLine;

    private void TextEditor_OnMouseHover(object sender, MouseEventArgs e)
    {
        TextViewPosition? position = textEditor.GetPositionFromPoint(Mouse.GetPosition(textEditor));
        if (position == null) return;
        TextLocation location = position.Value.Location;
        DocumentLine line = textEditor.Document.GetLineByNumber(location.Line);
        string lineText = textEditor.Text.Substring(line.Offset, line.Length);
        foreach (FunctionDefinition func in XmlData.FunctionDefinitions)
        {
            string searchString = $"{func.Name}(";
            List<int> results = lineText.AllIndexesOf(searchString);
            if (results.Count == 0) continue;
            foreach (int index in results)
            {
                if (location.Column >= index && location.Column < index + searchString.Length)
                {
                    ToolTip funcToolTip = (ToolTip)textEditor.ToolTip;
                    funcToolTip.Content = new FunctionToolTip(func);
                    funcToolTip.IsOpen = true;
                    return;
                }
            }
        }
    }

    private void TextEditor_OnMouseMove(object sender, MouseEventArgs e)
    {
        ToolTip funcToolTip = (ToolTip)textEditor.ToolTip;
        funcToolTip.IsOpen = false;
    }

    private void TextEditor_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        TextViewPosition? position = textEditor.GetPositionFromPoint(Mouse.GetPosition(textEditor));
        if (position == null) return;
        TextLocation location = position.Value.Location;
        DocumentLine line = textEditor.Document.GetLineByNumber(location.Line);
        string lineText = textEditor.Text.Substring(line.Offset, line.Length);
        foreach (FunctionDefinition func in XmlData.FunctionDefinitions)
        {
            string searchString = $"{func.Name}(";
            List<int> results = lineText.AllIndexesOf(searchString);
            if (results.Count == 0) continue;
            foreach (int index in results)
            {
                if (location.Column >= index && location.Column < index + searchString.Length)
                {
                    DetailedFunctionView.SetFunctionToDisplay(func);
                    return;
                }

                DetailedFunctionView.ClearDisplay();
            }
        }
    }

    private void TextEditor_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control) return;
        
        if (e.Delta > 0)
        {
            textEditor.FontSize += 1.0;
            textEditor.ScrollToVerticalOffset(textEditor.VerticalOffset + textEditor.FontSize * 2);
        }
        else if (textEditor.FontSize > 1.0)
        {
            textEditor.FontSize -= 1.0;
            textEditor.ScrollToVerticalOffset(textEditor.VerticalOffset - textEditor.FontSize * 2);
        }

        e.Handled = true;
    }
}