using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ppp_trade.Models;
using ppp_trade.ViewModels;

namespace ppp_trade.Services;

public class OverlayWindowService(IServiceProvider serviceProvider)
{
    private OverlayWindow? _currentItemOverlay;
    private OverlayRegexWindow? _currentRegexOverlay;

    public void CloseItemOverlay()
    {
        if (_currentItemOverlay == null)
        {
            return;
        }

        Application.Current.Dispatcher.Invoke(() => { _currentItemOverlay.Close(); });
    }

    public void CloseRegexOverlay()
    {
        if (_currentRegexOverlay == null)
        {
            return;
        }

        Application.Current.Dispatcher.Invoke(() => { _currentRegexOverlay.Close(); });
    }

    public void ShowItemOverlay(ItemBase item, GameInfo gameInfo)
    {
        CloseItemOverlay();

        Application.Current.Dispatcher.Invoke(() =>
        {
            var viewModel = new OverlayWindowViewModel(this, new OverlayWindowViewModel.DisplayOption
            {
                Item = item,
                GameInfo = gameInfo,
                CloseOnMouseMove = true
            });
            _currentItemOverlay = new OverlayWindow
            {
                DataContext = viewModel,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            _currentItemOverlay.Closed += (_, _) => { _currentItemOverlay = null; };

            _currentItemOverlay.Show();
        });
    }

    public void ShowRegexOverlay()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_currentRegexOverlay != null)
            {
                _currentRegexOverlay.Activate();
                return;
            }

            var viewModel = serviceProvider.GetRequiredService<OverlayRegexWindowViewModel>();
            _currentRegexOverlay = new OverlayRegexWindow
            {
                DataContext = viewModel,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            _currentRegexOverlay.Closed += (_, _) => _currentRegexOverlay = null;

            _currentRegexOverlay.Show();
            _currentRegexOverlay.Activate();
        });
    }
}