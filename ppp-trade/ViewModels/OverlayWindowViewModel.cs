using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveCharts;
using LiveCharts.Wpf;

namespace ppp_trade.ViewModels;

public partial class OverlayWindowViewModel : ObservableObject
{
    public OverlayWindowViewModel()
    {
        if (App.ServiceProvider == null!)
        {
            MatchedItemVisibility = Visibility.Visible;
            MatchedCurrencyVisibility = Visibility.Visible;

            MatchedItem = new MatchedItemVM
            {
                Count = 42,
                MatchedItemImage =
                    "https://web.poecdn.com/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQXJtb3Vycy9IZWxtZXRzL01hc2tDcm93biIsInciOjIsImgiOjIsInNjYWxlIjoxfV0/dbad72643e/MaskCrown.png",
                PriceAnalysisVMs =
                [
                    new PriceAnalysisVM
                    {
                        Price = 10, Count = 3,
                        CurrencyImageUrl =
                            "https://web.poecdn.com/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lSZXJvbGxSYXJlIiwic2NhbGUiOjF9XQ/46a2347805/CurrencyRerollRare.png"
                    },
                    new PriceAnalysisVM
                    {
                        Price = 15, Count = 1,
                        CurrencyImageUrl =
                            "https://web.poecdn.com/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lSZXJvbGxSYXJlIiwic2NhbGUiOjF9XQ/46a2347805/CurrencyRerollRare.png"
                    }
                ]
            };

            MatchedCurrency = new MatchedCurrencyVM
            {
                MatchedCurrencyImage =
                    "https://web.poecdn.com/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lSZXJvbGxSYXJlIiwic2NhbGUiOjF9XQ/46a2347805/CurrencyRerollRare.png",
                PayCurrencyImage =
                    "https://web.poecdn.com/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lSZXJvbGxSYXJlIiwic2NhbGUiOjF9XQ/46a2347805/CurrencyRerollRare.png",
                ExchangeRateList =
                [
                    new ExchangeRate
                    {
                        Value = 160.5,
                        CurrencyImageUrl =
                            "https://web.poecdn.com/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lSZXJvbGxSYXJlIiwic2NhbGUiOjF9XQ/46a2347805/CurrencyRerollRare.png"
                    }
                ],
                SeriesCollection =
                [
                    new LineSeries { Values = new ChartValues<double> { 150, 155, 160, 158, 162 } }
                ],
                Labels = ["12/01", "12/02", "12/03", "12/04", "12/05"],
                YFormatter = val => val.ToString("N1")
            };
        }
    }

    [ObservableProperty]
    private MatchedCurrencyVM? _matchedCurrency;

    [ObservableProperty]
    private Visibility _matchedCurrencyVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private MatchedItemVM? _matchedItem;

    [ObservableProperty]
    private Visibility _matchedItemVisibility = Visibility.Collapsed;

    public class ExchangeRate
    {
        public string? CurrencyImageUrl { get; set; }

        public double Value { get; set; }
    }

    public class MatchedCurrencyVM
    {
        public string? MatchedCurrencyImage { get; set; }

        public string? PayCurrencyImage { get; set; }

        public List<ExchangeRate> ExchangeRateList { get; set; } = [];

        public SeriesCollection SeriesCollection { get; set; } = [];

        public ObservableCollection<string> Labels { get; set; } = [];

        public Func<double, string>? YFormatter { get; set; }
    }

    public class MatchedItemVM
    {
        public int Count { get; set; }

        public string? MatchedItemImage { get; set; }

        public string? QueryId { get; set; }

        public List<PriceAnalysisVM> PriceAnalysisVMs { get; set; } = [];
    }

    public class PriceAnalysisVM
    {
        public string? Currency { get; set; }

        public double Price { get; set; }

        public string? CurrencyImageUrl { get; set; }

        public int Count { get; set; }
    }
}