using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using HandyControl.Data;
using ppp_trade.Enums;
using ppp_trade.Models;
using ppp_trade.Services;

namespace ppp_trade.ViewModels;

public partial class SettingWindowViewModel : ObservableObject
{
    public SettingWindowViewModel(CacheService cacheService)
    {
        _cacheService = cacheService;
        LoadSettings();
    }

    private const string SettingsFileName = "regex_settings.json";
    private const string CacheKey = "RegexSettings";

    private const string Poe1MapHazardSettingsFileName = "poe1_map_hazard_settings.json";
    private const string Poe1MapHazardCacheKey = "Poe1MapHazardSettings";

    private const string Poe2MapHazardSettingsFileName = "poe2_map_hazard_settings.json";
    private const string Poe2MapHazardCacheKey = "Poe2MapHazardSettings";

    private readonly CacheService _cacheService;

    [ObservableProperty]
    private ObservableCollection<MapHazardSetting> _poe1MapHazardSettings = [];

    [ObservableProperty]
    private ObservableCollection<MapHazardSetting> _poe2MapHazardSettings = [];

    [ObservableProperty]
    private ObservableCollection<RegexSetting> _regexSettings = [];

    [ObservableProperty]
    private RegexSetting? _selectedRegexSetting;

    public IList<HazardLevel> HazardLevels { get; } = Enum.GetValues<HazardLevel>();

    [RelayCommand]
    private void AddRegex()
    {
        var newSetting = new RegexSetting { Name = "New Regex" };
        RegexSettings.Add(newSetting);
        SelectedRegexSetting = newSetting;
    }

    [RelayCommand]
    private void DeleteRegex()
    {
        if (SelectedRegexSetting != null)
        {
            RegexSettings.Remove(SelectedRegexSetting);
            SelectedRegexSetting = null;
        }
    }

    [RelayCommand]
    private void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(RegexSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFileName, json);
            _cacheService.Set(CacheKey, RegexSettings);
            Growl.Success(new GrowlInfo
            {
                Message = "設定已儲存",
                Token = "LogMsg",
                WaitTime = 2
            });
        }
        catch (Exception ex)
        {
            Growl.Error(new GrowlInfo
            {
                Message = $"儲存失敗: {ex.Message}",
                Token = "LogMsg",
                WaitTime = 2
            });
        }
    }

    [RelayCommand]
    private void SaveMapHazardSettings()
    {
        try
        {
            var json1 = JsonSerializer.Serialize(Poe1MapHazardSettings,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Poe1MapHazardSettingsFileName, json1);
            _cacheService.Set(Poe1MapHazardCacheKey, Poe1MapHazardSettings);

            var json2 = JsonSerializer.Serialize(Poe2MapHazardSettings,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Poe2MapHazardSettingsFileName, json2);
            _cacheService.Set(Poe2MapHazardCacheKey, Poe2MapHazardSettings);

            Growl.Success(new GrowlInfo
            {
                Message = "地圖詞綴設定已儲存",
                Token = "LogMsg",
                WaitTime = 2
            });
        }
        catch (Exception ex)
        {
            Growl.Error(new GrowlInfo
            {
                Message = $"儲存失敗: {ex.Message}",
                Token = "LogMsg",
                WaitTime = 2
            });
        }
    }

    private void InitializeDefaultMapHazards()
    {
        // todo example list for both games for now, can be specialized later
        var defaultHazards = new List<MapHazardSetting>
        {
            new() { Id = "reflect_phys", Stat = "怪物反射 % 物理傷害", HazardLevel = HazardLevel.SAFE },
            new() { Id = "reflect_ele", Stat = "怪物反射 % 元素傷害", HazardLevel = HazardLevel.SAFE },
            new() { Id = "no_regen", Stat = "玩家無法回復生命、魔力或護盾", HazardLevel = HazardLevel.SAFE },
            new() { Id = "less_recovery", Stat = "玩家回復率更加無效", HazardLevel = HazardLevel.SAFE },
            new() { Id = "no_leech", Stat = "怪物無法被偷取", HazardLevel = HazardLevel.SAFE }
        };

        // Initialize POE 1
        if (Poe1MapHazardSettings.Any())
        {
            foreach (var defaultHazard in defaultHazards)
            {
                var existing = Poe1MapHazardSettings.FirstOrDefault(x => x.Id == defaultHazard.Id);
                if (existing != null)
                {
                    defaultHazard.HazardLevel = existing.HazardLevel;
                }
            }
        }

        Poe1MapHazardSettings = new ObservableCollection<MapHazardSetting>(defaultHazards.Select(x =>
            new MapHazardSetting { Id = x.Id, Stat = x.Stat, HazardLevel = x.HazardLevel }));

        foreach (var h in defaultHazards)
        {
            h.HazardLevel = HazardLevel.SAFE;
        }

        if (Poe2MapHazardSettings.Any())
        {
            foreach (var defaultHazard in defaultHazards)
            {
                var existing = Poe2MapHazardSettings.FirstOrDefault(x => x.Id == defaultHazard.Id);
                if (existing != null)
                {
                    defaultHazard.HazardLevel = existing.HazardLevel;
                }
            }
        }

        Poe2MapHazardSettings = new ObservableCollection<MapHazardSetting>(defaultHazards.Select(x =>
            new MapHazardSetting { Id = x.Id, Stat = x.Stat, HazardLevel = x.HazardLevel }));
    }


    private void LoadSettings()
    {
        if (_cacheService.TryGet(CacheKey, out ObservableCollection<RegexSetting>? cachedSettings) &&
            cachedSettings != null)
        {
            RegexSettings = cachedSettings;
        }
        else if (File.Exists(SettingsFileName))
        {
            try
            {
                var json = File.ReadAllText(SettingsFileName);
                var loaded = JsonSerializer.Deserialize<ObservableCollection<RegexSetting>>(json);
                if (loaded != null)
                {
                    RegexSettings = loaded;
                    _cacheService.Set(CacheKey, loaded);
                }
            }
            catch
            {
                // ignore
            }
        }

        // Load POE 1
        if (_cacheService.TryGet(Poe1MapHazardCacheKey, out ObservableCollection<MapHazardSetting>? cachedPoe1) &&
            cachedPoe1 != null)
        {
            Poe1MapHazardSettings = cachedPoe1;
        }
        else if (File.Exists(Poe1MapHazardSettingsFileName))
        {
            try
            {
                var json = File.ReadAllText(Poe1MapHazardSettingsFileName);
                var loaded = JsonSerializer.Deserialize<ObservableCollection<MapHazardSetting>>(json);
                if (loaded != null)
                {
                    Poe1MapHazardSettings = loaded;
                    _cacheService.Set(Poe1MapHazardCacheKey, loaded);
                }
            }
            catch
            {
                /* ignore */
            }
        }

        // Load POE 2
        if (_cacheService.TryGet(Poe2MapHazardCacheKey, out ObservableCollection<MapHazardSetting>? cachedPoe2) &&
            cachedPoe2 != null)
        {
            Poe2MapHazardSettings = cachedPoe2;
        }
        else if (File.Exists(Poe2MapHazardSettingsFileName))
        {
            try
            {
                var json = File.ReadAllText(Poe2MapHazardSettingsFileName);
                var loaded = JsonSerializer.Deserialize<ObservableCollection<MapHazardSetting>>(json);
                if (loaded != null)
                {
                    Poe2MapHazardSettings = loaded;
                    _cacheService.Set(Poe2MapHazardCacheKey, loaded);
                }
            }
            catch
            {
                /* ignore */
            }
        }

        InitializeDefaultMapHazards();
    }
}