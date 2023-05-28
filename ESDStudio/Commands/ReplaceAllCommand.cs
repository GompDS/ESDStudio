using System.Collections.Generic;
using System.Text.RegularExpressions;
using ESDStudio.ViewModels;
using ESDStudio.Views;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace ESDStudio.Commands;

public class ReplaceAllCommand : CommandBase
{
    public ReplaceAllCommand(ESDView esd, string find, string replace, List<Match> matches)
    {
        _esd = esd;
        _esdVM = (ESDViewModel)esd.DataContext;
        _oldCaretOffset = _esd.CodeEditor.CaretOffset;
        _find = find;
        _replace = replace;
        _matches = matches;
    }
    
    private ESDView _esd;
    private ESDViewModel _esdVM;
    private List<Match> _matches;
    private int _oldCaretOffset;
    private string _find;
    private string _replace;
    
    public override void Redo()
    {
        int startOffset = 0;
        for (int i = 0; i < _matches.Count; i++)
        {
            _esd.CodeEditor.Document.Replace(_matches[i].Index + startOffset, _find.Length, _replace);
            startOffset += _matches[i].Index + _replace.Length;
            _esd.CodeEditor.CaretOffset = startOffset;
        }
    }
    
    public override void Undo()
    {
        int startOffset = 0;
        for (int i = 0; i < _matches.Count; i++)
        {
            _esd.CodeEditor.Document.Replace(_matches[i].Index + startOffset, _replace.Length, _find);
            startOffset += _matches[i].Index + _find.Length;
        }
        
        _esd.CodeEditor.CaretOffset = _oldCaretOffset;

        _esdVM.ESDEditCount -= _matches.Count;
    }
}