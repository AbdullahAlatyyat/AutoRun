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

    private const byte VK_LSHIFT = 0xA0;
    private const byte VK_W = 0x57;

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
            SendKey(VK_W, keyDown: false);
            SendKey(VK_LSHIFT, keyDown: false);
            holding = false;
            Console.WriteLine("AutoRun: OFF");
        }
        else
        {
            SendKey(VK_LSHIFT, keyDown: true);
            SendKey(VK_W, keyDown: true);
            holding = true;
            Console.WriteLine("AutoRun: ON  (holding Shift+W)");
        }
    }

    private void Quit()
    {
        if (holding)
        {
            SendKey(VK_W, keyDown: false);
            SendKey(VK_LSHIFT, keyDown: false);
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

    // Sends a real hardware scan code via SendInput rather than a synthetic
    // virtual-key event (keybd_event), because most games poll raw scan
    // codes through DirectInput and never see keybd_event's key state.
    private static void SendKey(byte vk, bool keyDown)
    {
        ushort scan = (ushort)MapVirtualKey(vk, MAPVK_VK_TO_VSC);
        var input = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = scan,
                    dwFlags = KEYEVENTF_SCANCODE | (keyDown ? 0 : KEYEVENTF_KEYUP),
                    time = 0,
                    dwExtraInfo = IntPtr.Zero,
                },
            },
        };
        SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    private const uint MAPVK_VK_TO_VSC = 0;
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_SCANCODE = 0x0008;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
