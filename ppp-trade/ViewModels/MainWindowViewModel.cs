using System.Diagnostics;
using System.IO;
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
            _itemInfoVisibility = Visibility.Visible;
            _matchedItemVisibility = Visibility.Visible;
            _parsedItemVM = new ItemVM
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
        _cacheService = App.ServiceProvider.GetRequiredService<CacheService>();
        _gameStringService = App.ServiceProvider.GetRequiredService<GameStringService>();
        _rateLimitParser = App.ServiceProvider.GetRequiredService<RateLimitParser>();
        _iconService = App.ServiceProvider.GetRequiredService<IconService>();
        _mapper = App.ServiceProvider.GetRequiredService<IMapper>();
        _selectedServer = _serverList[1];
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

    private readonly CacheService _cacheService = null!;

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
    private Visibility _itemInfoVisibility = Visibility.Hidden;

    [ObservableProperty]
    private IList<string> _leagueList = [];

    [ObservableProperty]
    private MatchedItemVM? _matchedItem;

    [ObservableProperty]
    private Visibility _matchedItemVisibility = Visibility.Hidden;

    private Item? _parsedItem;

    [ObservableProperty]
    private ItemVM? _parsedItemVM;

    private CancellationTokenSource _queryCts = new();

    [ObservableProperty]
    private string? _rateLimitMessage;

    [ObservableProperty]
    private Visibility _rateLimitVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private IList<string> _selectableRarity = null!;

    [ObservableProperty]
    private CollapseByAccount _selectedCollapseState = CollapseByAccount.NO;

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

    private Task _waitTimeTask = Task.CompletedTask;

    private async Task<MatchedItemVM> AnalysisPriceAsync(object queryObj, string league)
    {
        JsonSerializerOptions serializerOptions = new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
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

    private async Task<(string? uniqueName, string? uniqueBase)> MapUniqueNameAsync(string legendName, string legendBase)
    {
        var nameMapCacheKey = "unique:tw2en:name";
        var baseMapCacheKey = "unique:tw2en:base";
        _cacheService.TryGet(baseMapCacheKey, out Dictionary<string, string>? baseMap);
        if (!_cacheService.TryGet(nameMapCacheKey, out Dictionary<string, string>? nameMap))
        {
            var enNameFile = Path.Combine("configs", "unique_item_names_eng.json");
            var twNameFile = Path.Combine("configs", "unique_item_names_tw.json");
            var enBaseFile = Path.Combine("configs", "unique_item_bases_eng.json");
            var twBaseFile = Path.Combine("configs", "unique_item_bases_tw.json");
            if (!File.Exists(enNameFile) ||
                !File.Exists(twNameFile) ||
                !File.Exists(enBaseFile) ||
                !File.Exists(twBaseFile))
            {
                return (null, null);
            }

            var content = await File.ReadAllTextAsync(enNameFile);
            var enNameList = JsonSerializer.Deserialize<List<string>>(content)!;
            content = await File.ReadAllTextAsync(twNameFile);
            var twNameList = JsonSerializer.Deserialize<List<string>>(content)!;
            nameMap = new Dictionary<string, string>();
            for (var i = 0; i < twNameList.Count; i++)
            {
                nameMap.Add(twNameList[i], enNameList[i]);
            }

            _cacheService.Set(nameMapCacheKey, nameMap);

            content = await File.ReadAllTextAsync(enBaseFile);
            var enBaseList = JsonSerializer.Deserialize<List<string>>(content)!;
            content = await File.ReadAllTextAsync(twBaseFile);
            var twBaseList = JsonSerializer.Deserialize<List<string>>(content)!;
            baseMap = new Dictionary<string, string>();
            for (var i = 0; i < twBaseList.Count; i++)
            {
                baseMap.Add(twBaseList[i], enBaseList[i]);
            }

            _cacheService.Set(baseMapCacheKey, baseMap);
        }

        return (nameMap![legendName], baseMap![legendBase]);
    }

    private IEnumerable<object> GetStatsQueryParam()
    {
        var statList = new List<object>();
        Debug.Assert(ParsedItemVM != null);
        foreach (var statVM in ParsedItemVM.StatVMs)
        {
            statList.Add(new
            {
                id = statVM.Id,
                disabled = !statVM.IsSelected,
                value = statVM.MinValue == null && statVM.MaxValue == null
                    ? null
                    : new
                    {
                        min = statVM.MinValue,
                        max = statVM.MaxValue
                    }
            });
        }


        return
        [
            new
            {
                type = "and",
                filters = statList
            }
        ];
    }

    private async Task<object> GetQueryParam()
    {
        Debug.Assert(ParsedItemVM != null);
        Debug.Assert(_parsedItem != null);
        var queryItem = _parsedItem.Value;

        var tradeTypeIndex = TradeTypeList.IndexOf(SelectedTradeType!);
        var tradeType = tradeTypeIndex switch
        {
            0 => "available",
            1 => "securable",
            2 => "online",
            _ => "any"
        };
        string? itemName = null;
        string? baseName = null;
        if (queryItem.Rarity == Rarity.UNIQUE)
        {
            (itemName, baseName) = await MapUniqueNameAsync(queryItem.ItemName, queryItem.ItemBase);
            if (itemName == null)
            {
                throw new FileNotFoundException("缺失傳奇道具文字相關檔案");
            }
        }
        else if(ParsedItemVM.FilterItemBase)
        {
            // todo create item base mapping
            baseName = queryItem.ItemBase;
        }

        var statsParam = GetStatsQueryParam().ToList();

        return new
        {
            status = new
            {
                option = tradeType
            },
            name = itemName,
            type = baseName,
            stats = statsParam.Count == 0 ? null : statsParam,
            filters = new
            {
                type_filters = new
                {
                    disabled = false,
                    filters = new
                    {
                        rarity = new
                        {
                            option = RarityToString(queryItem.Rarity)
                        },
                        category = new
                        {
                            option = ItemTypeToString(queryItem.ItemType)
                        }
                    }
                },
                misc_filters = new
                {
                    disabled = false,
                    filters = new
                    {
                        corrupted = SelectedCorruptedState switch
                        {
                            CorruptedState.ANY => null,
                            CorruptedState.YES => "yes",
                            _ => "no"
                        }
                    }
                },
                trade_filters = new
                {
                    filters = new
                    {
                        sale_type = new
                        {
                            option = "priced"
                        },
                        collapse = new
                        {
                            option = SelectedCollapseState == CollapseByAccount.YES ? "yes" : "no"
                        }
                    }
                }
            }
        };
    }

    private string? ItemTypeToString(ItemType itemType)
    {
        return itemType switch
        {
            ItemType.HELMET => "armour.helmet",
            ItemType.ONE_HAND_AXE => "weapon.oneaxe",
            ItemType.ONE_HAND_MACE => "weapon.onemace",
            ItemType.ONE_HAND_SWORD => "weapon.onesword",
            ItemType.BOW => "weapon.bow",
            ItemType.CLAW => "weapon.claw",
            ItemType.DAGGER => "weapon.basedagger",
            ItemType.RUNE_DAGGER => "weapon.runedagger",
            ItemType.SCEPTRE => "weapon.sceptre",
            ItemType.STAFF => "weapon.staff",
            ItemType.TWO_HAND_AXE => "weapon.twoaxe",
            ItemType.TWO_HAND_MACE => "weapon.twomace",
            ItemType.TWO_HAND_SWORD => "weapon.twosword",
            ItemType.WAND => "weapon.wand",
            ItemType.FISHING_ROD => "weapon.rod",
            ItemType.BODY_ARMOUR => "armour.chest",
            ItemType.BOOTS => "armour.boots",
            ItemType.GLOVES => "armour.gloves",
            ItemType.SHIELD => "armour.shield",
            ItemType.Quiver => "armour.quiver",
            ItemType.AMULET => "accessory.amulet",
            ItemType.BELT => "accessory.belt",
            ItemType.RING => "accessory.ring",
            _ => null
        };
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
            LeagueList = leagues.Where(l => l.Realm == "pc").Select(l => l.Text).ToList();
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

    private ItemVM MapItemToView(Item item)
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
            });
        });
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

        ParsedItemVM = MapItemToView(_parsedItem.Value);
        ItemInfoVisibility = Visibility.Visible;
    }

    partial void OnSelectedServerChanged(string? value)
    {
        var domain = value == "台服" ? "https://www.pathofexile.tw/" : "https://www.pathofexile.com/";
        _poeApiService.SwitchDomain(domain);
        LoadLeagues().ConfigureAwait(false);
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
            MatchedItemVisibility = Visibility.Hidden;
            CanQuery = false;
            if (ParsedItemVM == null || _parsedItem == null)
            {
                return;
            }

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

            // todo complete method
            object queryParam;
            try
            {
                queryParam = await GetQueryParam();
            }
            catch (Exception e)
            {
                Growl.Warning(new GrowlInfo
                {
                    Message = e.Message,
                    Token = "LogMsg",
                    WaitTime = 2
                });
                return;
            }

            var query = new
            {
                query = queryParam,
                sort = new
                {
                    price = "asc"
                }
            };
            try
            {
                MatchedItem = await AnalysisPriceAsync(query, SelectedLeague!);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            MatchedItemVisibility = Visibility.Visible;
        }
        finally
        {
            CanQuery = true;
        }
    }

    private string? RarityToString(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.NORMAL => "normal",
            Rarity.UNIQUE => "unique",
            Rarity.MAGIC => "magic",
            Rarity.RARE => "rare",
            _ => null
        };
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

    public class ItemVM
    {
        public string? ItemName { get; set; }

        public int? ItemLevelMin { get; set; }

        public int? ItemLevelMax { get; set; }

        public bool FilterItemLevel { get; set; } = true;

        public bool FilterRarity { get; set; } = true;

        public bool FilterLink { get; set; } = true;

        public bool FilterItemBase { get; set; }

        public string? Rarity { get; set; }

        public int? LinkCountMin { get; set; }

        public int? LinkCountMax { get; set; }

        public string? ItemBase { get; set; }

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
}