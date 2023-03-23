using System.Windows;

namespace ESDStudio;

public static class DialogCloser
{
    public static readonly DependencyProperty DialogResultProperty = 
        DependencyProperty.RegisterAttached(
        "DialogResult", 
        typeof(bool?), 
        typeof(DialogCloser), 
        new PropertyMetadata(DialogResultChanged));
    private static void DialogResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Window window)
            window.DialogResult = e.NewValue as bool?;
    }
    public static void SetDialogResult(Window target, bool? value)
    {
        target.SetValue(DialogResultProperty, value);
    }
}