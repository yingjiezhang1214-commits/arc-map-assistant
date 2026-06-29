using System.Runtime.InteropServices;

namespace ArcMapAssistant.Core.Services;

public sealed class HotkeyService : IDisposable
{
    private readonly IntPtr _windowHandle;
    private readonly HashSet<int> _registeredIds = new();

    public HotkeyService(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
    }

    public bool Register(int id, string hotkey)
    {
        if (!TryParseFunctionKey(hotkey, out var virtualKey))
        {
            return false;
        }

        if (!RegisterHotKey(_windowHandle, id, 0, virtualKey))
        {
            return false;
        }

        _registeredIds.Add(id);
        return true;
    }

    public void Dispose()
    {
        foreach (var id in _registeredIds)
        {
            UnregisterHotKey(_windowHandle, id);
        }

        _registeredIds.Clear();
    }

    private static bool TryParseFunctionKey(string hotkey, out uint virtualKey)
    {
        virtualKey = 0;

        if (!hotkey.StartsWith("F", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!int.TryParse(hotkey[1..], out var functionNumber) || functionNumber is < 1 or > 24)
        {
            return false;
        }

        virtualKey = (uint)(0x70 + functionNumber - 1);
        return true;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

