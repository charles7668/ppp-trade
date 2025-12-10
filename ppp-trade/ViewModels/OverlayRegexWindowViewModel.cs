using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ppp_trade.Models;
using ppp_trade.Services;

namespace ppp_trade.ViewModels;

public partial class OverlayRegexWindowViewModel : ObservableObject
{
    public OverlayRegexWindowViewModel(ClipboardMonitorService clipboardService, CacheService cacheService)
    {
        _clipboardService = clipboardService;
        _cacheService = cacheService;
        LoadSettings();
    }

    private const string SettingsFileName = "regex_settings.json";
    private const string CacheKey = "RegexSettings";
    private readonly CacheService _cacheService;
    private readonly ClipboardMonitorService _clipboardService;

    [ObservableProperty]
    private ObservableCollection<RegexSetting> _regexSettings = [];

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
                // Ignore load errors
            }
        }
    }

    [RelayCommand]
    private void OnRegexDoubleClick(RegexSetting? setting)
    {
        if (setting == null || string.IsNullOrEmpty(setting.Regex))
        {
            return;
        }

        try
        {
            // First send Ctrl+F to focus the search box in game (common POE workflow)
            // But user only asked for "copy regex" previously. 
            // The prompt "button command to double click" might imply just triggering the copy action on double click.
            // Let's stick to copy for now.
            Clipboard.SetText(setting.Regex);
        }
        catch (Exception ex)
        {
            // Handle clipboard exception if any
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }
}