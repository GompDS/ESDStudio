using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ESDStudio;

public class CodeEditorTab
{
    public Button EnterButton { get; }
    public TextBox TextEditor { get; }
    public ESDInfo ESD { get; }

    public CodeEditorTab(ESDInfo esdInfo)
    {
        ESD = esdInfo;
        Style tabStyle = new Style(typeof(Button));
        tabStyle.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, 40.0));
        tabStyle.Setters.Add(new Setter(FrameworkElement.MaxWidthProperty, 160.0));
        tabStyle.Setters.Add(new Setter(FrameworkElement.MaxHeightProperty, 30.0));
        tabStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.White));
        EnterButton = new Button
        {
            Style = tabStyle,
            Content = ESD.Id,
            Margin = new Thickness(2.0, 0.0, 2.0, 0.0),
            HorizontalContentAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        EnterButton.SetValue(Grid.ColumnProperty, 2);
        TextEditor = new TextBox
        {
            AcceptsTab = true,
            AcceptsReturn = true,
            BorderThickness = new Thickness(2.0),
            FontFamily = Fonts.SystemFontFamilies.First(x => x.Source == "Courier New"),
            Text = ESD.Code,
            Visibility = Visibility.Hidden,
            IsEnabled = false,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        TextEditor.SetValue(Grid.ColumnProperty, 2);
        TextEditor.SetValue(Grid.RowProperty, 1);
    }

    public void Hide()
    {
        TextEditor.Visibility = Visibility.Hidden;
        TextEditor.IsEnabled = false;
        EnterButton.Background = Brushes.LightGray;
    }
    
    public void Show()
    {
        TextEditor.Visibility = Visibility.Visible;
        TextEditor.IsEnabled = true;
        EnterButton.Background = Brushes.White;
        if (ESD.IsEdited)
        {
            EnterButton.Content = ESD.Id + "*";
        }
    }
}