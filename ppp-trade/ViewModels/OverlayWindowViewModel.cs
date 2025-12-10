using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Windows;
using AutoMapper;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Extensions.DependencyInjection;
using ppp_trade.Builders;
using ppp_trade.Enums;
using ppp_trade.Models;
using ppp_trade.Services;

namespace ppp_trade.ViewModels;

public partial class OverlayWindowViewModel : ObservableObject
{
    public OverlayWindowViewModel(OverlayWindowService windowService, DisplayOption displayOption) : this()
    {
        _displayOption = displayOption;
        _overlayWindowService = windowService;
        _isQuerying = true;

        switch (displayOption.GameInfo.Game)
        {
            case "POE1":
                _poe1ItemVM = _mapper.Map<Poe1ItemVM>(_displayOption.Item);
                break;
            case "POE2":
                _poe2ItemVM = _mapper.Map<Poe2ItemVM>(_displayOption.Item);
                break;
        }

        if (_displayOption.CloseOnMouseMove)
        {
            StartMonitoring();
        }
    }

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
                    new ExchangeRateVM
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
        else
        {
            _poeApiService = App.ServiceProvider.GetRequiredService<PoeApiService>();
            _iconService = App.ServiceProvider.GetRequiredService<IconService>();
            _rateLimitParser = App.ServiceProvider.GetRequiredService<RateLimitParser>();
            _mapper = App.ServiceProvider.GetRequiredService<IMapper>();
        }
    }

    private readonly DisplayOption _displayOption = new();
    private readonly IconService _iconService = null!;
    private readonly IMapper _mapper = null!;

    private readonly OverlayWindowService? _overlayWindowService;

    private readonly Poe1ItemVM? _poe1ItemVM;
    private readonly Poe2ItemVM? _poe2ItemVM;
    private readonly PoeApiService _poeApiService = null!;
    private readonly RateLimitParser _rateLimitParser;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private Visibility _errorVisibility;

    [ObservableProperty]
    private bool _isQuerying;

    [ObservableProperty]
    private MatchedCurrencyVM? _matchedCurrency;

    [ObservableProperty]
    private Visibility _matchedCurrencyVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private MatchedItemVM? _matchedItem;

    [ObservableProperty]
    private Visibility _matchedItemVisibility = Visibility.Collapsed;

    private Point _previousMousePoint;
    private CancellationTokenSource _queryCts = new();

    [ObservableProperty]
    private string? _rateLimitMessage;

    [ObservableProperty]
    private Visibility _rateLimitVisibility = Visibility.Collapsed;

    private Task _waitTimeTask = Task.CompletedTask;

    private async Task<MatchedItemVM> AnalysisPriceAsync(object queryObj)
    {
        var (game, league, _) = (_displayOption.GameInfo.Game, _displayOption.GameInfo.League,
            _displayOption.GameInfo.Server);
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
                var currency = group.Key.currency;
                analysis.Add(new PriceAnalysisVM
                {
                    Count = group.Count(),
                    Currency = group.Key.currency,
                    Price = group.Key.amount,
                    CurrencyImageUrl = _iconService.GetCurrencyIcon(currency, game)
                });
            }
        }
        catch (TaskCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            throw;
        }

        matchItem.PriceAnalysisVMs = analysis;

        return matchItem;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(ref Win32Point pt);

    [RelayCommand]
    private async Task OnLoaded()
    {
        try
        {
            await Query();
        }
        catch
        {
            // ignore
        }
        finally
        {
            IsQuerying = false;
        }
    }

    private async Task Query()
    {
        try
        {
            MatchedItemVisibility = Visibility.Collapsed;
            MatchedCurrencyVisibility = Visibility.Collapsed;

            switch (_displayOption.Item.ItemType)
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
            ErrorMessage = ex.Message;
            ErrorVisibility = Visibility.Visible;
        }
    }

    private async Task QueryCurrency()
    {
        if (_displayOption.GameInfo.Server != "國際服")
        {
            ErrorMessage = "通貨匯率查詢只支援國際服";
            ErrorVisibility = Visibility.Visible;
            return;
        }

        var (game, league, server) = (_displayOption.GameInfo.Game, _displayOption.GameInfo.League,
            _displayOption.GameInfo.Server);

        var item = _displayOption.Item;
        var nameMappingService = App.ServiceProvider.GetRequiredService<NameMappingService>();
        var currencyName =
            await nameMappingService.MapBaseItemNameAsync(item.ItemBaseName, game);
        if (currencyName == null)
        {
            ErrorMessage = "無法解析通貨名";
            ErrorVisibility = Visibility.Visible;
            return;
        }

        var response = await _poeApiService.GetCurrencyExchangeRate(currencyName, league, game);
        var imgUrl = response["item"]?["image"]?.ToString();
        var matchedCurrency = new MatchedCurrencyVM
        {
            MatchedCurrencyImage = "https://web.poecdn.com" + imgUrl,
            YFormatter = value => value.ToString("F4")
        };
        var exchangeList = response["pairs"]?.AsArray();
        if (exchangeList == null)
        {
            ErrorMessage = "沒有此通貨匯率資料";
            ErrorVisibility = Visibility.Visible;
            return;
        }

        var cores = response["core"]?["items"]?.AsArray()!;

        foreach (var exchange in exchangeList)
        {
            imgUrl = (from core in cores
                where core?["id"]?.ToString() == exchange?["id"]?.ToString()
                select core?["image"]?.ToString()).FirstOrDefault();

            var rate = exchange?["rate"]!.ToString();
            matchedCurrency.ExchangeRateList.Add(new ExchangeRateVM
            {
                CurrencyImageUrl = "https://web.poecdn.com" + imgUrl,
                Value = double.Parse(rate!)
            });
        }

        if (matchedCurrency.ExchangeRateList.Count > 0)
        {
            matchedCurrency.PayCurrencyImage = matchedCurrency.ExchangeRateList[0].CurrencyImageUrl;
        }

        var history = exchangeList.First()?["history"]?.AsArray();
        if (history != null)
        {
            var rateHistories = history.Select(x => double.Parse(x?["rate"]!.ToString()!)).Reverse().ToList();
            var dateHistories = history
                .Select(x => DateTime.Parse(x?["timestamp"]!.ToString()!).ToString("MM/dd")).Reverse().ToList();

            if (rateHistories.Count > 0)
            {
                var values = new ChartValues<double>();
                values.AddRange(rateHistories);

                matchedCurrency.SeriesCollection.Add(new LineSeries
                {
                    Title = "Rate",
                    Values = values,
                    PointGeometry = null
                });

                foreach (var date in dateHistories)
                {
                    matchedCurrency.Labels.Add(date);
                }
            }
        }

        MatchedCurrency = matchedCurrency;
        MatchedCurrencyVisibility = Visibility.Visible;
    }

    private async Task QueryItem()
    {
        // todo complete method
        var tradeType = "securable";
        var (game, league, server) = (_displayOption.GameInfo.Game, _displayOption.GameInfo.League,
            _displayOption.GameInfo.Server);
        SearchRequestBase searchRequest;
        if (game == "POE1")
        {
            searchRequest = _mapper.Map<Poe1SearchRequest>(_poe1ItemVM, opt =>
            {
                opt.AfterMap((_, dest) =>
                {
                    dest.ServerOption = server == "台服"
                        ? ServerOption.TAIWAN_SERVER
                        : ServerOption.INTERNATIONAL_SERVER;
                    dest.TradeType = tradeType;
                    dest.CorruptedState = CorruptedState.NO;
                    dest.CollapseByAccount = CollapseByAccount.YES;
                    dest.Item = _displayOption.Item;
                });
            });
        }
        else
        {
            searchRequest = _mapper.Map<Poe2SearchRequest>(_poe2ItemVM, opt =>
            {
                opt.AfterMap((_, dest) =>
                {
                    dest.ServerOption = server == "台服"
                        ? ServerOption.TAIWAN_SERVER
                        : ServerOption.INTERNATIONAL_SERVER;
                    dest.TradeType = tradeType;
                    dest.CorruptedState = CorruptedState.NO;
                    dest.CollapseByAccount = CollapseByAccount.YES;
                    dest.Item = _displayOption.Item;
                });
            });
        }

        var builder = App.ServiceProvider.GetRequiredService<RequestBodyBuilder>();
        var searchBody = await builder.BuildSearchBodyAsync(searchRequest, game);
        if (searchBody == null)
        {
            ErrorMessage = "無法建立搜尋參數";
            ErrorVisibility = Visibility.Visible;
            return;
        }

        try
        {
            MatchedItem = await AnalysisPriceAsync(searchBody);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        MatchedItemVisibility = Visibility.Visible;
    }

    private void StartMonitoring()
    {
        Task.Factory.StartNew(async () =>
        {
            var pt = new Win32Point();
            GetCursorPos(ref pt);
            _previousMousePoint = new Point(pt.X, pt.Y);

            while (true)
            {
                await Task.Delay(100);
                GetCursorPos(ref pt);
                var current = new Point(pt.X, pt.Y);
                if (Point.Subtract(current, _previousMousePoint).Length > 50)
                {
                    await _queryCts.CancelAsync();
                    _overlayWindowService?.CloseItemOverlay();
                    break;
                }
            }
        }, TaskCreationOptions.LongRunning);
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

    [StructLayout(LayoutKind.Sequential)]
    private struct Win32Point
    {
        public int X;
        public int Y;
    }

    public class DisplayOption
    {
        public ItemBase Item { get; set; }

        public GameInfo GameInfo { get; set; }

        public bool CloseOnMouseMove { get; set; }
    }
}