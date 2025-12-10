using System.Windows;

namespace ppp_trade;

public partial class OverlayRegexWindow : Window
{
    public OverlayRegexWindow()
    {
        InitializeComponent();
        Deactivated += (s, e) => Close();
    }
}