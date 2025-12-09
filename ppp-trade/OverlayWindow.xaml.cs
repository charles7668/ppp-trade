using ppp_trade.ViewModels;

namespace ppp_trade;

/// <summary>
/// OverlayWindow.xaml 的互動邏輯
/// </summary>
public partial class OverlayWindow
{
    public OverlayWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    private readonly OverlayWindowViewModel _viewModel = new();
}