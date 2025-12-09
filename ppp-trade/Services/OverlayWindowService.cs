using System.Windows;
using ppp_trade.ViewModels;

namespace ppp_trade.Services;

public class OverlayWindowService
{
    private OverlayWindow? _currentOverlay;

    public void Close()
    {
        if (_currentOverlay == null)
        {
            return;
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            _currentOverlay.Close();
            _currentOverlay = null;
        });
    }

    public void Show()
    {
        Close();

        Application.Current.Dispatcher.Invoke(() =>
        {
            var viewModel = new OverlayWindowViewModel(this, new OverlayWindowViewModel.DisplayOption
            {
                CloseOnMouseMove = true
            });
            _currentOverlay = new OverlayWindow
            {
                DataContext = viewModel,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            _currentOverlay.Show();
        });
    }
}