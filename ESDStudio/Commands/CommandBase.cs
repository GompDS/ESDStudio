using System;
using System.Windows.Input;

namespace ESDStudio.Commands;

public class CommandBase : ICommand
{
    public virtual bool CanExecute(object? parameter)
    {
        return true;
    }

    public virtual void Execute(object? parameter)
    {
        throw new NotImplementedException();
    }

    public virtual void Undo()
    {
        throw new NotImplementedException();
    }

    public event EventHandler? CanExecuteChanged;
}