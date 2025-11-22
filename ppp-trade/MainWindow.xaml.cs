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
        DataContext = _viewModel;
    }

    private readonly MainWindowViewModel _viewModel = new();

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

        if (dep is DataGridRow { Item: MainWindowViewModel.ItemStatVM item })
        {
            item.IsSelected = !item.IsSelected;
            e.Handled = true;
        }
    }
}