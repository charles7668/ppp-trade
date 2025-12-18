using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ppp_trade.Models;
using ppp_trade.Services;

namespace ppp_trade.ViewModels;

public partial class OverlayRegexWindowViewModel : ObservableObject
{
    public OverlayRegexWindowViewModel(CacheService cacheService,
        OverlayWindowService overlayWindowService)
    {
        _cacheService = cacheService;
        _overlayWindowService = overlayWindowService;
        LoadSettings();
    }

    private const string SettingsFileName = "regex_settings.json";
    private const string CacheKey = "RegexSettings";

    private const int KEYEVENTF_KEYUP = 0x0002;
    private const int VK_BACK = 0x08;
    private const int VK_CONTROL = 0x11;
    private const int VK_F = 0x46;
    private const int VK_V = 0x56;
    private const int WM_IME_CONTROL = 0x0283;
    private const int IMC_SETOPENSTATUS = 0x0006;
    private readonly CacheService _cacheService;
    private readonly OverlayWindowService _overlayWindowService;

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
    private async Task OnRegexMouseDown(MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2)
        {
            return;
        }

        var element = e.OriginalSource as FrameworkElement;
        if (element?.DataContext is not RegexSetting setting || string.IsNullOrEmpty(setting.Regex))
        {
            return;
        }

        try
        {
            Clipboard.SetText(setting.Regex);

            if (FocusPoeWindow())
            {
                // Wait for window focus
                await Task.Delay(100);

                // Ctrl + F
                keybd_event(VK_CONTROL, 0, 0, 0);
                keybd_event(VK_F, 0, 0, 0);
                keybd_event(VK_F, 0, KEYEVENTF_KEYUP, 0);
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);

                await Task.Delay(50);

                // Backspace
                keybd_event(VK_BACK, 0, 0, 0);
                keybd_event(VK_BACK, 0, KEYEVENTF_KEYUP, 0);

                await Task.Delay(50);

                // Ctrl + V
                keybd_event(VK_CONTROL, 0, 0, 0);
                keybd_event(VK_V, 0, 0, 0);
                keybd_event(VK_V, 0, KEYEVENTF_KEYUP, 0);
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
            }

            _overlayWindowService.CloseRegexOverlay();
        }
        catch (Exception ex)
        {
            // Handle clipboard exception if any
            Debug.WriteLine(ex.Message);
        }
    }

    private static bool FocusPoeWindow()
    {
        var processNames = new[] { "PathOfExile", "PathOfExile_x64" };
        foreach (var name in processNames)
        {
            var processes = Process.GetProcessesByName(name);
            foreach (var handle in processes.Select(process => process.MainWindowHandle))
            {
                if (handle != IntPtr.Zero)
                {
                    SetForegroundWindow(handle);

                    var imeHandle = ImmGetDefaultIMEWnd(handle);
                    if (imeHandle != IntPtr.Zero)
                    {
                        SendMessage(imeHandle, WM_IME_CONTROL, IMC_SETOPENSTATUS, IntPtr.Zero);
                    }

                    return true;
                }
            }
        }

        return false;
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

    [DllImport("imm32.dll")]
    private static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
}