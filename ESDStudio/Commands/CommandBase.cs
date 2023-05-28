using System;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Document;

namespace ESDStudio.Commands;

public class CommandBase : IUndoableOperation
{
    public virtual void Redo()
    {
        throw new NotImplementedException();
    }

    public virtual void Undo()
    {
        throw new NotImplementedException();
    }
}