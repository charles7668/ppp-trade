using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using HandyControl.Data;
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
    private readonly CacheService _cacheService;

    [ObservableProperty]
    private ObservableCollection<RegexSetting> _regexSettings = [];

    [ObservableProperty]
    private RegexSetting? _selectedRegexSetting;

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

    private void LoadSettings()
    {
        if (_cacheService.TryGet(CacheKey, out ObservableCollection<RegexSetting>? cachedSettings) &&
            cachedSettings != null)
        {
            RegexSettings = cachedSettings;
            return;
        }

        if (File.Exists(SettingsFileName))
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
                // Ignore load errors or log them
            }
        }
    }
}