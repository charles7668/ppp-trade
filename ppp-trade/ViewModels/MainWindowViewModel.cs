using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using HandyControl.Data;
using Microsoft.Extensions.DependencyInjection;
using ppp_trade.Enums;
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
            return;
        }

        _poeApiService = App.ServiceProvider.GetRequiredService<PoeApiService>();
        _clipboardMonitorService = App.ServiceProvider.GetRequiredService<ClipboardMonitorService>();
        _parserFactory = App.ServiceProvider.GetRequiredService<ParserFactory>();
        _selectedServer = _serverList[1];
        OnSelectedServerChanged(_selectedServer);
        _selectedTradeType = _tradeTypeList[1];
    }

    private readonly ClipboardMonitorService _clipboardMonitorService = null!;

    private readonly ParserFactory _parserFactory = null!;

    private readonly PoeApiService _poeApiService = null!;

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
        }
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