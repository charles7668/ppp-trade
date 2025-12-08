using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Windows;
using AutoMapper;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using HandyControl.Data;
using Microsoft.Extensions.DependencyInjection;
using ppp_trade.Builders;
using ppp_trade.Enums;
using ppp_trade.Models;
using ppp_trade.Models.Parsers;
using ppp_trade.Services;

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
            _poe1ItemInfoVisibility = Visibility.Visible;
            _poe2ItemInfoVisibility = Visibility.Visible;
            _matchedItemVisibility = Visibility.Visible;
            _matchedCurrencyVisibility = Visibility.Visible;
            _parsedPoe1ItemVM = new ItemVM
            {
                ItemName = "Design Time Item Name",
                StatVMs =
                [
                    new ItemStatVM
                    {
                        StatText = "測試1",
                        Type = "隨機"
                    }
                ]
            };
            _parsedPoe2ItemVM = new Poe2ItemVM
            {
                ItemName = "Design Time Poe2 Item Name",
                StatVMs =
                [
                    new ItemStatVM
                    {
                        StatText = "測試1",
                        Type = "隨機"
                    }
                ]
            };

            _matchedItem = new MatchedItemVM
            {
                Count = 0,
                MatchedItemImage =
                    "https://web.poecdn.com/gen/image/WzI1LDE0LHsiZiI6IjJESXRlbXMvQXJtb3Vycy9IZWxtZXRzL01hc2tDcm93biIsInciOjIsImgiOjIsInNjYWxlIjoxfV0/dbad72643e/MaskCrown.png",
                PriceAnalysisVMs =
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
                    }
                ]
            };
            _rateLimitMessage = "請求過於頻繁，需等待 0 秒...";
            _rateLimitVisibility = Visibility.Visible;
            return;
        }

        _poeApiService = App.ServiceProvider.GetRequiredService<PoeApiService>();
        _clipboardMonitorService = App.ServiceProvider.GetRequiredService<ClipboardMonitorService>();
        _parserFactory = App.ServiceProvider.GetRequiredService<ParserFactory>();
        _gameStringService = App.ServiceProvider.GetRequiredService<GameStringService>();
        _rateLimitParser = App.ServiceProvider.GetRequiredService<RateLimitParser>();
        _iconService = App.ServiceProvider.GetRequiredService<IconService>();
        _mapper = App.ServiceProvider.GetRequiredService<IMapper>();
        _selectedServer = _serverList[1];
        _selectedGame = _gameList[0];
        _selectableRarity =
        [
            _gameStringService.Get(GameString.NORMAL)!,
            _gameStringService.Get(GameString.MAGIC)!,
            _gameStringService.Get(GameString.RARE)!,
            _gameStringService.Get(GameString.UNIQUE)!
        ];
        OnSelectedServerChanged(_selectedServer);
        _selectedTradeType = _tradeTypeList[1];
    }

    private readonly ClipboardMonitorService _clipboardMonitorService = null!;

    private readonly GameStringService _gameStringService = null!;

    private readonly IconService _iconService = null!;

    private readonly IMapper _mapper = null!;

    private readonly ParserFactory _parserFactory = null!;

    private readonly PoeApiService _poeApiService = null!;

    private readonly RateLimitParser _rateLimitParser = null!;

    [ObservableProperty]
    private bool _canQuery = true;

    [ObservableProperty]
    private IList<string> _gameList = ["POE1", "POE2"];

    [ObservableProperty]
    private IList<string> _leagueList = [];

    [ObservableProperty]
    private MatchedCurrencyVM? _matchedCurrency;

    [ObservableProperty]
    private Visibility _matchedCurrencyVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private MatchedItemVM? _matchedItem;

    [ObservableProperty]
    private Visibility _matchedItemVisibility = Visibility.Collapsed;

    private ItemBase? _parsedItem;

    [ObservableProperty]
    private ItemVM? _parsedPoe1ItemVM;

    [ObservableProperty]
    private Poe2ItemVM? _parsedPoe2ItemVM;

    [ObservableProperty]
    private Visibility _poe1ItemInfoVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _poe2ItemInfoVisibility = Visibility.Collapsed;

    private CancellationTokenSource _queryCts = new();

    [ObservableProperty]
    private ObservableCollection<QueryHistoryItem> _queryHistory = [];

    [ObservableProperty]
    private string? _rateLimitMessage;

    [ObservableProperty]
    private Visibility _rateLimitVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private IList<string> _selectableRarity = null!;

    [ObservableProperty]
    private IList<YesNoAnyOption> _selectableYesNoAny = [YesNoAnyOption.ANY, YesNoAnyOption.YES, YesNoAnyOption.NO];

    [ObservableProperty]
    private CollapseByAccount _selectedCollapseState = CollapseByAccount.NO;

    [ObservableProperty]
    private CorruptedState _selectedCorruptedState = CorruptedState.ANY;

    [ObservableProperty]
    private string? _selectedGame;

    [ObservableProperty]
    private QueryHistoryItem? _selectedHistoryItem;

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

    private Task _waitTimeTask = Task.CompletedTask;

    private async Task<MatchedItemVM> AnalysisPriceAsync(object queryObj, string league)
    {
        Debug.Assert(SelectedGame != null);
        var serializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        var json = JsonSerializer.Serialize(queryObj, serializerOptions);
        Debug.WriteLine(json);
        List<PriceAnalysisVM> analysis = [];
        var matchItem = new MatchedItemVM();
        try
        {
            _queryCts = new CancellationTokenSource();
            await _waitTimeTask;
            var fetched = await _poeApiService.GetTradeSearchResultAsync(league, json);
            Debug.WriteLine(JsonSerializer.Serialize(fetched));
            if (!fetched.ContainsKey("id") || !fetched.ContainsKey("result"))
            {
                throw new ArgumentException("search result not contain 'id' or 'result' field");
            }

            var needWaitTime = _rateLimitParser.GetWaitTimeForRateLimit(fetched["rate-limit"].Deserialize<string?>(),
                fetched["rate-limit-state"].Deserialize<string?>());

            await WaitWithCountdown(needWaitTime, _queryCts.Token);

            var queryId = fetched["id"]!.ToString();
            var results = fetched["result"].Deserialize<List<string>>()!;
            var total = fetched["total"]!.GetValue<int>();
            matchItem.Count = total;
            matchItem.QueryId = queryId;

            List<JsonNode> nodes = [];
            for (var i = 0; i < 4; i++)
            {
                var end = (i + 1) * 10;
                List<string> fetchIds = [];
                for (var j = i * 10; j < Math.Min(end, results.Count); j++)
                {
                    fetchIds.Add(results[j]);
                }

                if (fetchIds.Count == 0)
                {
                    continue;
                }

                var fetchItems = await _poeApiService.FetchItems(fetchIds, queryId);
                needWaitTime = _rateLimitParser.GetWaitTimeForRateLimit(
                    fetched["rate-limit"].Deserialize<string?>(),
                    fetched["rate-limit-state"].Deserialize<string?>());

                await WaitWithCountdown(needWaitTime, _queryCts.Token);
                if (!fetchItems.ContainsKey("result"))
                {
                    throw new ArgumentException("fetch result not contain 'result' field");
                }

                nodes.AddRange(fetchItems["result"]!.AsArray()!);
            }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            matchItem.MatchedItemImage = null;
            if (nodes.Count > 0)
            {
                matchItem.MatchedItemImage = nodes[0]["item"]["icon"].ToString();
            }

            var priceInfos = nodes.Select(x =>
                new
                {
                    type = x["listing"]["price"]["type"].ToString(),
                    amount = x["listing"]["price"]["amount"].GetValue<int>(),
                    currency = x["listing"]["price"]["currency"].ToString()
                });
            var groups = priceInfos.GroupBy(x => (x.amount, x.currency));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            foreach (var group in groups)
            {
                var currency = JudgeCurrency(group.Key.currency);
                analysis.Add(new PriceAnalysisVM
                {
                    Count = group.Count(),
                    Currency = group.Key.currency,
                    Price = group.Key.amount,
                    CurrencyImageUrl = _iconService.GetCurrencyIcon(currency)
                });
            }
        }
        catch (TaskCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Growl.Warning(new GrowlInfo
            {
                Message = ex.Message,
                Token = "LogMsg",
                WaitTime = 2
            });
            Debug.WriteLine(ex);
        }

        matchItem.PriceAnalysisVMs = analysis;

        return matchItem;
    }

    [RelayCommand]
    private void ClearHistory()
    {
        QueryHistory.Clear();
    }

    private void ClearParsedData()
    {
        _clipboardMonitorService.ClearClipboard();
        Poe1ItemInfoVisibility = Visibility.Collapsed;
        Poe2ItemInfoVisibility = Visibility.Collapsed;
        ParsedPoe1ItemVM = null;
        ParsedPoe2ItemVM = null;
        _parsedItem = null;
    }

    private Currency? JudgeCurrency(string currencyText)
    {
        if (string.IsNullOrWhiteSpace(currencyText))
        {
            return null;
        }

        // normalize text to upper case
        var standardizedName = currencyText.Replace('-', '_').ToUpperInvariant();

        var parseState = Enum.TryParse(standardizedName, out Currency currency);
        if (parseState)
        {
            return currency;
        }

        return null;
    }

    private async Task LoadLeagues()
    {
        try
        {
            var leagues = await _poeApiService.GetLeaguesAsync();
            LeagueList = SelectedGame == "POE1"
                ? leagues.Where(l => l.Realm == "pc").Select(l => l.Text).ToList()
                : leagues.Select(l => l.Text).ToList();

            if (LeagueList.Any())
            {
                SelectedLeague = LeagueList[0];
            }
        }
        catch
        {
            // todo show error message
        }
    }

    private ItemVM MapPoe1ItemToView(Poe1Item item)
    {
        return _mapper.Map<ItemVM>(item, opt =>
        {
            opt.AfterMap((_, dest) =>
            {
                dest.Rarity = item.Rarity switch
                {
                    Rarity.MAGIC => _gameStringService.Get(GameString.MAGIC)!,
                    Rarity.RARE => _gameStringService.Get(GameString.RARE)!,
                    Rarity.UNIQUE => _gameStringService.Get(GameString.UNIQUE)!,
                    _ => _gameStringService.Get(GameString.NORMAL)!
                };
                dest.FoulBorn = item.IsFoulBorn ? YesNoAnyOption.YES : YesNoAnyOption.NO;
            });
        });
    }

    private Poe2ItemVM MapPoe2ItemToView(Poe2Item item)
    {
        return _mapper.Map<Poe2ItemVM>(item, opt =>
        {
            opt.AfterMap((_, dest) =>
            {
                dest.Rarity = item.Rarity switch
                {
                    Rarity.MAGIC => _gameStringService.Get(GameString.MAGIC)!,
                    Rarity.RARE => _gameStringService.Get(GameString.RARE)!,
                    Rarity.UNIQUE => _gameStringService.Get(GameString.UNIQUE)!,
                    _ => _gameStringService.Get(GameString.NORMAL)!
                };
            });
        });
    }

    private void OnClipboardChanged(object? sender, string clipboardText)
    {
        Debug.WriteLine($"Clipboard content:\n {clipboardText}");
        Debug.Assert(SelectedGame != null);
        var parser = _parserFactory.GetParser(clipboardText, SelectedGame);
        if (parser == null)
        {
            return;
        }

        Poe1ItemInfoVisibility = Visibility.Collapsed;
        Poe2ItemInfoVisibility = Visibility.Collapsed;

        try
        {
            _parsedItem = parser.Parse(clipboardText);
        }
        catch
        {
            _parsedItem = null;
        }

        switch (_parsedItem)
        {
            case null:
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
            case Poe1Item:
                ParsedPoe1ItemVM = MapPoe1ItemToView((Poe1Item)_parsedItem);
                Poe1ItemInfoVisibility = Visibility.Visible;
                break;
            default:
                ParsedPoe2ItemVM = MapPoe2ItemToView((Poe2Item)_parsedItem);
                Poe2ItemInfoVisibility = Visibility.Visible;
                break;
        }
    }

    partial void OnSelectedGameChanged(string? value)
    {
        _poeApiService.SwitchGame(value ?? "POE1");
        LoadLeagues().ConfigureAwait(false);
        ClearParsedData();
    }

    partial void OnSelectedHistoryItemChanged(QueryHistoryItem? value)
    {
        if (value == null)
        {
            return;
        }

        if (value.MatchedItem != null)
        {
            MatchedItem = value.MatchedItem;
        }

        MatchedItemVisibility = Visibility.Visible;
    }

    partial void OnSelectedServerChanged(string? value)
    {
        var domain = value == "台服" ? "https://pathofexile.tw/" : "https://www.pathofexile.com/";
        _poeApiService.SwitchDomain(domain);
        LoadLeagues().ConfigureAwait(false);
        ClearParsedData();
    }

    [RelayCommand]
    private void OpenAbout()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/charles7668/ppp-trade",
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private void OpenTradeSite()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = _poeApiService.GetSearchWebsiteUrl(MatchedItem?.QueryId ?? "", SelectedLeague ?? ""),
            UseShellExecute = true
        });
    }

    [RelayCommand(CanExecute = nameof(CanQuery))]
    private async Task Query()
    {
        try
        {
            MatchedItemVisibility = Visibility.Collapsed;
            MatchedCurrencyVisibility = Visibility.Collapsed;
            CanQuery = false;
            Debug.Assert(SelectedGame != null);

            if (SelectedLeague == null)
            {
                Growl.Warning(new GrowlInfo
                {
                    Message = "請先選擇聯盟",
                    Token = "LogMsg",
                    WaitTime = 2
                });
                return;
            }

            ItemType? checkItemType = null;
            checkItemType ??= _parsedItem?.ItemType;
            if (checkItemType == null)
            {
                Growl.Warning(new GrowlInfo
                {
                    Message = "尚未解析道具",
                    Token = "LogMsg",
                    WaitTime = 2
                });
                return;
            }

            switch (checkItemType)
            {
                case ItemType.STACKABLE_CURRENCY:
                    await QueryCurrency();
                    break;
                default:
                    await QueryItem();
                    break;
            }
        }
        catch (Exception ex)
        {
            ShowWarning(ex.Message);
        }
        finally
        {
            CanQuery = true;
        }
    }

    private async Task QueryCurrency()
    {
        // todo complete
        Debug.Assert(_parsedItem != null);
        if (SelectedServer != "國際服")
        {
            ShowWarning("通貨匯率查詢只支援國際服");
            return;
        }

        var nameMappingService = App.ServiceProvider.GetRequiredService<NameMappingService>();
        var currencyName = await nameMappingService.MapBaseItemNameAsync(_parsedItem.ItemBaseName, SelectedGame!);
        if (currencyName == null)
        {
            ShowWarning("無法解析通貨名");
            return;
        }

        var response = await _poeApiService.GetCurrencyExchangeRate(currencyName, SelectedLeague!, SelectedGame!);
        var imgUrl = response["item"]?["image"]?.ToString();
        var matchedCurrency = new MatchedCurrencyVM
        {
            MatchedCurrencyImage = "https://web.poecdn.com" + imgUrl
        };
        var exchangeList = response["pairs"]?.AsArray();
        if (exchangeList == null)
        {
            ShowWarning("沒有此通貨匯率資料");
            return;
        }

        var cores = response["core"]?["items"]?.AsArray()!;

        foreach (var exchange in exchangeList)
        {
            imgUrl = (from core in cores
                where core?["id"]?.ToString() == exchange?["id"]?.ToString()
                select core?["image"]?.ToString()).FirstOrDefault();

            var rate = exchange?["rate"]!.ToString();
            matchedCurrency.ExchangeRateList.Add(new ExchangeRate
            {
                CurrencyImageUrl = "https://web.poecdn.com" + imgUrl,
                Value = double.Parse(rate!)
            });
        }

        MatchedCurrency = matchedCurrency;
        MatchedCurrencyVisibility = Visibility.Visible;
    }

    private async Task QueryItem()
    {
        // todo complete method
        var tradeTypeIndex = TradeTypeList.IndexOf(SelectedTradeType!);
        var tradeType = tradeTypeIndex switch
        {
            0 => "available",
            1 => "securable",
            2 => "online",
            _ => "any"
        };
        SearchRequestBase searchRequest;
        if (SelectedGame == "POE1")
        {
            searchRequest = _mapper.Map<Poe1SearchRequest>(ParsedPoe1ItemVM, opt =>
            {
                opt.AfterMap((_, dest) =>
                {
                    dest.ServerOption = SelectedServer == "台服"
                        ? ServerOption.TAIWAN_SERVER
                        : ServerOption.INTERNATIONAL_SERVER;
                    dest.TradeType = tradeType;
                    dest.CorruptedState = SelectedCorruptedState;
                    dest.CollapseByAccount = SelectedCollapseState;
                    dest.Item = _parsedItem;
                });
            });
        }
        else
        {
            searchRequest = _mapper.Map<Poe2SearchRequest>(ParsedPoe2ItemVM, opt =>
            {
                opt.AfterMap((_, dest) =>
                {
                    dest.ServerOption = SelectedServer == "台服"
                        ? ServerOption.TAIWAN_SERVER
                        : ServerOption.INTERNATIONAL_SERVER;
                    dest.TradeType = tradeType;
                    dest.CorruptedState = SelectedCorruptedState;
                    dest.CollapseByAccount = SelectedCollapseState;
                    dest.Item = _parsedItem;
                });
            });
        }

        var builder = App.ServiceProvider.GetRequiredService<RequestBodyBuilder>();
        var searchBody = await builder.BuildSearchBodyAsync(searchRequest, SelectedGame!);
        if (searchBody == null)
        {
            Growl.Warning(new GrowlInfo
            {
                Message = "無法建立搜尋參數",
                Token = "LogMsg",
                WaitTime = 2
            });
            return;
        }

        try
        {
            MatchedItem = await AnalysisPriceAsync(searchBody, SelectedLeague!);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        MatchedItemVisibility = Visibility.Visible;

        var itemName = SelectedGame == "POE1" ? ParsedPoe1ItemVM?.ItemName : ParsedPoe2ItemVM?.ItemName;
        Application.Current.Dispatcher.Invoke(() =>
        {
            QueryHistory.Insert(0, new QueryHistoryItem
            {
                ItemName = itemName,
                ItemImage = MatchedItem?.MatchedItemImage,
                QueryTime = DateTime.Now,
                Game = SelectedGame,
                League = SelectedLeague,
                ResultCount = MatchedItem?.Count ?? 0,
                MatchedItem = MatchedItem
            });
        });
    }

    private void ShowWarning(string msg)
    {
        Growl.Warning(new GrowlInfo
        {
            Message = msg,
            Token = "LogMsg",
            WaitTime = 2
        });
    }

    private async Task WaitWithCountdown(int waitTimeMs, CancellationToken ct)
    {
        if (waitTimeMs <= 0 || _waitTimeTask != Task.CompletedTask)
        {
            return;
        }

        _waitTimeTask = Task.Run(async () =>
        {
            RateLimitVisibility = Visibility.Visible;
            var seconds = (int)Math.Ceiling(waitTimeMs / 1000.0);
            for (var i = seconds; i > 0; i--)
            {
                RateLimitMessage = $"請求過於頻繁，需等待 {i} 秒...";
                await Task.Delay(1000, CancellationToken.None);
            }

            RateLimitVisibility = Visibility.Collapsed;
        }, CancellationToken.None);

        await Task.Delay(waitTimeMs, ct);
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

    public class ExchangeRate
    {
        public string? CurrencyImageUrl { get; set; }

        public double Value { get; set; }
    }

    public class MatchedCurrencyVM
    {
        public string? MatchedCurrencyImage { get; set; }

        public List<ExchangeRate> ExchangeRateList { get; set; } = [];
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

    public class Poe2ItemVM
    {
        public string? ItemName { get; set; }

        public int? ItemLevelMin { get; set; }

        public int? ItemLevelMax { get; set; }

        public bool FilterItemLevel { get; set; } = true;

        public bool FilterRarity { get; set; } = true;

        public bool FilterItemBase { get; set; } = true;

        public string? Rarity { get; set; }

        public string? ItemBaseName { get; set; }

        public bool FilterRunSockets { get; set; }

        public int? RunSocketsMin { get; set; }

        public int? RunSocketsMax { get; set; }

        public List<ItemStatVM> StatVMs { get; set; } = [];
    }

    public class ItemVM
    {
        public string? ItemName { get; set; }

        public int? ItemLevelMin { get; set; }

        public int? ItemLevelMax { get; set; }

        public bool FilterItemLevel { get; set; } = true;

        public bool FilterRarity { get; set; } = true;

        public bool FilterLink { get; set; } = true;

        public bool FilterGemLevel { get; set; }

        public bool FilterItemBase { get; set; } = true;

        public string? Rarity { get; set; }

        public int? LinkCountMin { get; set; }

        public int? LinkCountMax { get; set; }

        public int? GemLevelMin { get; set; }

        public int? GemLevelMax { get; set; }

        public string? ItemBaseName { get; set; }

        public YesNoAnyOption FoulBorn { get; set; } = YesNoAnyOption.ANY;

        public List<ItemStatVM> StatVMs { get; set; } = [];
    }

    public partial class ItemStatVM : ObservableObject
    {
        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private int? _maxValue;

        [ObservableProperty]
        private int? _minValue;

        public string? Id { get; set; }

        public string? Type { get; set; }

        public string? StatText { get; set; }
    }

    public class QueryHistoryItem
    {
        public string? ItemName { get; set; }

        public string? ItemImage { get; set; }

        public DateTime QueryTime { get; set; }

        public string? Game { get; set; }

        public string? League { get; set; }

        public int ResultCount { get; set; }

        public MatchedItemVM? MatchedItem { get; set; }
    }
}