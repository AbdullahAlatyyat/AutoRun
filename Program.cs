using System.Runtime.InteropServices;

namespace AutoRun;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Console.WriteLine("AutoRun is running.");
        Console.WriteLine($"  Press [{HotkeyWindow.ToggleKey}] to toggle holding Shift+W.");
        Console.WriteLine($"  Press [{HotkeyWindow.QuitKey}] to quit.");

        using var window = new HotkeyWindow();
        Application.Run();
    }
}

internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    public const Keys ToggleKey = Keys.F6;
    public const Keys QuitKey = Keys.F7;

    private const int WM_HOTKEY = 0x0312;
    private const int ToggleHotkeyId = 1;
    private const int QuitHotkeyId = 2;

    private const byte VK_SHIFT = 0x10;
    private const byte VK_W = 0x57;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private bool holding;

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams());
        RegisterHotKey(Handle, ToggleHotkeyId, 0, (uint)ToggleKey);
        RegisterHotKey(Handle, QuitHotkeyId, 0, (uint)QuitKey);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY)
        {
            switch (m.WParam.ToInt32())
            {
                case ToggleHotkeyId:
                    Toggle();
                    break;
                case QuitHotkeyId:
                    Quit();
                    break;
            }
        }

        base.WndProc(ref m);
    }

    private void Toggle()
    {
        if (holding)
        {
            keybd_event(VK_W, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            holding = false;
            Console.WriteLine("AutoRun: OFF");
        }
        else
        {
            keybd_event(VK_SHIFT, 0, 0, UIntPtr.Zero);
            keybd_event(VK_W, 0, 0, UIntPtr.Zero);
            holding = true;
            Console.WriteLine("AutoRun: ON  (holding Shift+W)");
        }
    }

    private void Quit()
    {
        if (holding)
        {
            keybd_event(VK_W, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        Console.WriteLine();
        Console.WriteLine("AutoRun: exiting");
        Application.Exit();
    }

    public void Dispose()
    {
        UnregisterHotKey(Handle, ToggleHotkeyId);
        UnregisterHotKey(Handle, QuitHotkeyId);
        DestroyHandle();
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
}
