using System.Windows;
using System.Windows.Input;

namespace ppp_trade;

public partial class OverlayRegexWindow : Window
{
    public OverlayRegexWindow()
    {
        InitializeComponent();
        PreviewKeyUp += (s, e) =>
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                Close();
            }
        };
    }
}