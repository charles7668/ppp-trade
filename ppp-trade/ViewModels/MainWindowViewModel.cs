using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using HandyControl.Data;
using Microsoft.Extensions.DependencyInjection;
using ppp_trade.Enums;
using ppp_trade.Models;
using ppp_trade.Models.Parsers;
using ppp_trade.Services;
using AutoMapper;

namespace ppp_trade.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel()
    {
        // if in design mode
        if (App.ServiceProvider == null!)
        {
            _selectedServer = _serverList[1];
            _selectedTradeType = _tradeTypeList[1];
            _itemInfoVisibility = Visibility.Visible;
            _matchedItemVisibility = Visibility.Visible;
            _parsedItemVM = new ItemVM()
            {
                ItemName = "Design Time Item Name",
                StatVMs =
                [
                    new ItemStatVM()
                    {
                        StatText = "測試1",
                        Type = "隨機"
                    }
                ]
            };

            _matchedItemImage =
                "https://web.poecdn.com/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQXJtb3Vycy9IZWxtZXRzL01hc2tDcm93biIsInciOjIsImgiOjIsInNjYWxlIjoxfV0/dbad72643e/MaskCrown.png";

            _priceAnalysisVMs =
            [
                new PriceAnalysisVM
                {
                    Price = 2,
                    CurrencyImageUrl =
                        "https://web.poecdn.com/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lSZXJvbGxSYXJlIiwic2NhbGUiOjF9XQ/46a2347805/CurrencyRerollRare.png",
                    Count = 2
                },
                new PriceAnalysisVM
                {
                    Price = 3,
                    CurrencyImageUrl =
                        "https://web.poecdn.com/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lSZXJvbGxSYXJlIiwic2NhbGUiOjF9XQ/46a2347805/CurrencyRerollRare.png",
                    Count = 2
                },
                new PriceAnalysisVM
                {
                    Price = 1,
                    CurrencyImageUrl =
                        "https://web.poecdn.com/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lSZXJvbGxSYXJlIiwic2NhbGUiOjF9XQ/46a2347805/CurrencyRerollRare.png",
                    Count = 1
                },
                new PriceAnalysisVM
                {
                    Price = 5,
                    CurrencyImageUrl =
                        "https://web.poecdn.com/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQ3VycmVuY3kvQ3VycmVuY3lSZXJvbGxSYXJlIiwic2NhbGUiOjF9XQ/46a2347805/CurrencyRerollRare.png",
                    Count = 1
                },
            ];
            
            return;
        }

        _poeApiService = App.ServiceProvider.GetRequiredService<PoeApiService>();
        _clipboardMonitorService = App.ServiceProvider.GetRequiredService<ClipboardMonitorService>();
        _parserFactory = App.ServiceProvider.GetRequiredService<ParserFactory>();
        _mapper = App.ServiceProvider.GetRequiredService<IMapper>();
        _selectedServer = _serverList[1];
        OnSelectedServerChanged(_selectedServer);
        _selectedTradeType = _tradeTypeList[1];
    }

    private readonly ClipboardMonitorService _clipboardMonitorService = null!;

    private readonly ParserFactory _parserFactory = null!;

    private readonly PoeApiService _poeApiService = null!;

    private readonly IMapper _mapper = null!;

    [ObservableProperty]
    private IList<string> _leagueList = [];

    [ObservableProperty]
    private CorruptedState _selectedCorruptedState = CorruptedState.ANY;

    [ObservableProperty]
    private string? _selectedLeague;

    [ObservableProperty]
    private string? _selectedServer;

    [ObservableProperty]
    private string? _selectedTradeType;

    [ObservableProperty]
    private IList<string> _serverList = ["台服", "國際服"];

    [ObservableProperty]
    private IList<string> _tradeTypeList = ["即刻購買以及面交", "僅限即刻購買", "僅限面交", "任何"];

    [ObservableProperty]
    private Visibility _itemInfoVisibility = Visibility.Hidden;

    [ObservableProperty]
    private Visibility _matchedItemVisibility = Visibility.Hidden;

    private Item? _parsedItem;

    [ObservableProperty]
    private ItemVM? _parsedItemVM;

    [ObservableProperty]
    private ObservableCollection<MatchedItemVM> _matchedItemVMs = [];

    [ObservableProperty]
    private string? _matchedItemImage;

    [ObservableProperty]
    private ObservableCollection<PriceAnalysisVM> _priceAnalysisVMs = [];

    public class MatchedItemVM
    {

    }

    public class PriceAnalysisVM
    {
        public double Price { get; set; }
        public string? CurrencyImageUrl { get; set; }
        public int Count { get; set; }
    }

    public class ItemVM
    {
        public string? ItemName { get; set; }

        public List<ItemStatVM> StatVMs { get; set; } = [];
    }

    public partial class ItemStatVM : ObservableObject
    {
        public string? Id { get; set; }

        public string? Type { get; set; }

        public string? StatText { get; set; }

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private int? _minValue;

        [ObservableProperty]
        private int? _maxValue;
    }

    private async Task LoadLeagues()
    {
        try
        {
            var leagues = await _poeApiService.GetLeaguesAsync();
            LeagueList = leagues.Where(l => l.Realm == "pc").Select(l => l.Text).ToList();
            if (LeagueList.Any())
                SelectedLeague = LeagueList[0];
        }
        catch
        {
            // todo show error message
        }
    }

    private void OnClipboardChanged(object? sender, string clipboardText)
    {
        Debug.WriteLine($"Clipboard content:\n {clipboardText}");
        var parser = _parserFactory.GetParser(clipboardText);
        if (parser == null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Growl.Warning(new GrowlInfo
                {
                    Message = "無法識別的物品格式",
                    Token = "LogMsg",
                    WaitTime = 2
                });
            });
            return;
        }

        ItemInfoVisibility = Visibility.Hidden;

        _parsedItem = parser.Parse(clipboardText);
        if (_parsedItem == null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Growl.Warning(new GrowlInfo
                {
                    Message = "無法識別的物品格式",
                    Token = "LogMsg",
                    WaitTime = 2
                });
            });
            return;
        }

        ParsedItemVM = _mapper.Map<ItemVM>(_parsedItem.Value);

        ItemInfoVisibility = Visibility.Visible;
    }

    partial void OnSelectedServerChanged(string? value)
    {
        var domain = value == "台服" ? "https://www.pathofexile.tw/" : "https://www.pathofexile.com/";
        _poeApiService.SwitchDomain(domain);
        LoadLeagues().ConfigureAwait(false);
    }

    [RelayCommand]
    private Task WindowClosing()
    {
        _clipboardMonitorService.StopMonitoring();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task WindowLoaded()
    {
        // todo load settings
        await LoadLeagues();
        _clipboardMonitorService.ClipboardChanged += OnClipboardChanged;
        _clipboardMonitorService.StartMonitoring();
    }
}