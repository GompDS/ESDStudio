using System.Text.RegularExpressions;
using ESDStudio.ViewModels;
using ESDStudio.Views;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace ESDStudio.Commands;

public class ReplaceCommand : CommandBase
{
    public ReplaceCommand(ESDView esd, string find, int replaceOffset, string replace)
    {
        _esd = esd;
        _esdVM = (ESDViewModel)esd.DataContext;
        _oldCaretOffset = _esd.CodeEditor.CaretOffset;
        _oldSelection = _esd.CodeEditor.TextArea.Selection;
        _find = find;
        _replaceOffset = replaceOffset;
        _replace = replace;
    }
    
    private ESDView _esd;
    private ESDViewModel _esdVM;
    private int _oldCaretOffset;
    private Selection _oldSelection;
    private string _find;
    private int _replaceOffset;
    private string _replace;
    
    public override void Redo()
    {
        _esd.CodeEditor.Document.Replace(_replaceOffset, _find.Length, _replace);
        _esd.CodeEditor.CaretOffset = _replaceOffset;
        _esd.CodeEditor.TextArea.Caret.BringCaretToView();
        Caret caret = _esd.CodeEditor.TextArea.Caret;
        TextViewPosition startPos = new(caret.Line, caret.Column, caret.VisualColumn);
        TextViewPosition endPos = new(caret.Line, caret.Column + _replace.Length, caret.VisualColumn + _replace.Length);
        _esd.CodeEditor.TextArea.Selection = new RectangleSelection(_esd.CodeEditor.TextArea, startPos, endPos);
    }
    
    public override void Undo()
    {
        _esd.CodeEditor.Document.Replace(_replaceOffset, _replace.Length, _find);
        _esd.CodeEditor.CaretOffset = _oldCaretOffset;
        _esd.CodeEditor.TextArea.Caret.BringCaretToView();
        _esd.CodeEditor.TextArea.Selection = _oldSelection;
        _esdVM.ESDEditCount--;
    }
}