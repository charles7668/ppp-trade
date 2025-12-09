using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace ppp_trade.Services;

public class GlobalHotkeyService
{
    private readonly Dictionary<int, Action> _callbacks = new();
    private int _currentId;
    private bool _isInitialized;

    private void ComponentDispatcher_ThreadPreprocessMessage(ref MSG msg, ref bool handled)
    {
        // ReSharper disable InconsistentNaming
        const int WM_HOTKEY = 0x0312;
        // ReSharper restore InconsistentNaming
        if (msg.message == WM_HOTKEY)
        {
            var id = (int)msg.wParam;
            if (_callbacks.TryGetValue(id, out var callback))
            {
                callback();
                handled = true;
            }
        }
    }

    private void Initialize()
    {
        ComponentDispatcher.ThreadPreprocessMessage += ComponentDispatcher_ThreadPreprocessMessage;
        _isInitialized = true;
    }

    public void Register(ModifierKeys modifier, Key key, Action callback)
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        var id = _currentId++;
        if (RegisterHotKey(IntPtr.Zero, id, (uint)modifier, (uint)KeyInterop.VirtualKeyFromKey(key)))
        {
            _callbacks.Add(id, callback);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    public void UnRegister()
    {
        if (_isInitialized)
        {
            ComponentDispatcher.ThreadPreprocessMessage -= ComponentDispatcher_ThreadPreprocessMessage;
            _isInitialized = false;
        }

        foreach (var id in _callbacks.Keys)
        {
            UnregisterHotKey(IntPtr.Zero, id);
        }

        _callbacks.Clear();
    }

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}