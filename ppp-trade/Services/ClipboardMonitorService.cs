using System.Diagnostics;
using System.Windows;

namespace ppp_trade.Services;

public class ClipboardMonitorService
{
    private CancellationTokenSource? _cts;
    private string _lastClipboardText = string.Empty;

    public Task ClearClipboard()
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                Clipboard.Clear();
                _lastClipboardText = string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("clipboard clear failed : " + ex.Message);
            }
        });
        return Task.CompletedTask;
    }

    public event EventHandler<string>? ClipboardChanged;

    private async Task MonitorClipboard(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var currentText = string.Empty;

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    currentText = Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;
                }
                catch
                {
                    // ignore
                }
            });

            if (!string.IsNullOrEmpty(currentText) && currentText != _lastClipboardText)
            {
                _lastClipboardText = currentText;
                ClipboardChanged?.Invoke(this, currentText);
            }

            await Task.Delay(500, token);
        }
    }

    public void StartMonitoring()
    {
        if (_cts != null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        Task.Factory.StartNew(() => MonitorClipboard(_cts.Token), _cts.Token, TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    public void StopMonitoring()
    {
        _cts?.Cancel();
        _cts = null;
    }
}