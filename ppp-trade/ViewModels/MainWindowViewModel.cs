using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
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
        _cacheService = App.ServiceProvider.GetRequiredService<CacheService>();
        _gameStringService = App.ServiceProvider.GetRequiredService<GameStringService>();
        _mapper = App.ServiceProvider.GetRequiredService<IMapper>();
        _selectedServer = _serverList[1];
        _selectableRarity =
        [
            _gameStringService.Get(GameString.NORMAL)!,
            _gameStringService.Get(GameString.MAGIC)!,
            _gameStringService.Get(GameString.RARE)!,
            _gameStringService.Get(GameString.UNIQUE)!,
        ];
        OnSelectedServerChanged(_selectedServer);
        _selectedTradeType = _tradeTypeList[1];
    }

    private readonly ClipboardMonitorService _clipboardMonitorService = null!;

    private readonly ParserFactory _parserFactory = null!;

    private readonly PoeApiService _poeApiService = null!;

    private readonly CacheService _cacheService = null!;

    private readonly GameStringService _gameStringService = null!;

    private readonly IMapper _mapper = null!;

    [ObservableProperty]
    private IList<string> _selectableRarity = null!;

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
    private string? _matchedItemImage;

    [ObservableProperty]
    private ObservableCollection<PriceAnalysisVM> _priceAnalysisVMs = [];

    public class PriceAnalysisVM
    {
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

        ParsedItemVM = MapItemToView(_parsedItem.Value);
        ItemInfoVisibility = Visibility.Visible;
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

    private async Task<object?> GetQueryParamForUnique()
    {
        var queryItem = _parsedItem!.Value;
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
                Growl.Warning(new GrowlInfo
                {
                    Message = "缺失傳奇道具文字相關檔案",
                    Token = "LogMsg",
                    WaitTime = 2
                });
                return null;
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

        var queryObj = new
        {
            status = "any",
            name = nameMap![_parsedItem.Value.ItemName],
            type = baseMap![_parsedItem.Value.ItemBase],
            filters = new
            {
                type_filters = new
                {
                    disabled = false,
                    filters = new
                    {
                        rarity = new
                        {
                            option = RarityToString(Rarity.UNIQUE)
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
                        corrupted = new
                        {
                            option = SelectedCorruptedState switch
                            {
                                CorruptedState.NO => "no",
                                CorruptedState.YES => "yes",
                                _ => "any"
                            }
                        }
                    }
                }
            }
        };
        return queryObj;
    }

    [RelayCommand]
    private async Task Query()
    {
        if (ParsedItemVM == null || _parsedItem == null)
            return;
        var queryItem = _parsedItem!.Value;
        // todo complete method
        if (queryItem.Rarity == Rarity.UNIQUE)
        {
            var queryParam = await GetQueryParamForUnique();
            if (queryParam == null)
                return;
        }
    }

    private string? RarityToString(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.UNIQUE => "unique",
            Rarity.MAGIC => "magic",
            Rarity.RARE => "rare",
            _ => null
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
}