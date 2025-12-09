using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ppp_trade.ViewModels;

namespace ppp_trade;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        Title = $"ppp-trade v{version}";
        DataContext = _viewModel;
    }

    private readonly MainWindowViewModel _viewModel = new();

    private void DataGrid_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;

        var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = MouseWheelEvent,
            Source = sender
        };

        var parent = ((DataGrid)sender).Parent as UIElement;
        parent?.RaiseEvent(eventArg);
    }

    private void DataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var dep = (DependencyObject)e.OriginalSource;

        while (dep != null && dep != sender)
        {
            if (dep is TextBox || dep is CheckBox)
            {
                return;
            }

            if (dep is DataGridRow)
            {
                break;
            }

            dep = VisualTreeHelper.GetParent(dep);
        }

        if (dep is DataGridRow { Item: ItemStatVM item })
        {
            item.IsSelected = !item.IsSelected;
            e.Handled = true;
        }
    }
}