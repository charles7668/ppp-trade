using System.Windows;
using ppp_trade.ViewModels;

namespace ppp_trade;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    private readonly MainWindowViewModel _viewModel = new();
}