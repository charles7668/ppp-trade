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
}