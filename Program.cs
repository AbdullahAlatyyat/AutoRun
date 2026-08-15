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
        Console.WriteLine("  Any other real key press releases the hold automatically.");

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

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const uint LLKHF_INJECTED = 0x00000010;

    private bool holding;
    private readonly LowLevelKeyboardProc keyboardHookProc;
    private readonly IntPtr keyboardHookHandle;

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams());
        RegisterHotKey(Handle, ToggleHotkeyId, 0, (uint)ToggleKey);
        RegisterHotKey(Handle, QuitHotkeyId, 0, (uint)QuitKey);

        // Kept as a field so the delegate isn't garbage-collected while the
        // unmanaged hook still holds a reference to it.
        keyboardHookProc = KeyboardHookCallback;
        keyboardHookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardHookProc, GetModuleHandle(null), 0);
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

    // Detects real (non-injected) key presses so a manual keystroke can
    // cancel the held Shift+W and hand control back to the player. The
    // low-level hook flags our own SendInput-generated presses as
    // "injected", so they're ignored here and never self-cancel.
    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && holding)
        {
            int message = wParam.ToInt32();
            if (message == WM_KEYDOWN || message == WM_SYSKEYDOWN)
            {
                var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                bool injected = (data.flags & LLKHF_INJECTED) != 0;
                var key = (Keys)data.vkCode;
                if (!injected && key != ToggleKey && key != QuitKey)
                {
                    StopHold("manual input detected");
                }
            }
        }

        return CallNextHookEx(keyboardHookHandle, nCode, wParam, lParam);
    }

    private void Toggle()
    {
        if (holding)
        {
            StopHold();
        }
        else
        {
            StartHold();
        }
    }

    private void StartHold()
    {
        SendKey(VK_LSHIFT, keyDown: true);
        SendKey(VK_W, keyDown: true);
        holding = true;
        Console.WriteLine("AutoRun: ON  (holding Shift+W)");
    }

    private void StopHold(string? reason = null)
    {
        if (!holding)
        {
            return;
        }

        SendKey(VK_W, keyDown: false);
        SendKey(VK_LSHIFT, keyDown: false);
        holding = false;
        Console.WriteLine(reason is null ? "AutoRun: OFF" : $"AutoRun: OFF ({reason})");
    }

    private void Quit()
    {
        StopHold();
        Console.WriteLine();
        Console.WriteLine("AutoRun: exiting");
        Application.Exit();
    }

    public void Dispose()
    {
        UnhookWindowsHookEx(keyboardHookHandle);
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

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

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

    // Size must be 32 on x64 (the padded size of the native union's largest
    // member, MOUSEINPUT) even though only KEYBDINPUT is used here.
    // Without it .NET sizes the union at 24 bytes, making INPUT 32 bytes
    // instead of the real 40, and SendInput silently rejects the whole call.
    [StructLayout(LayoutKind.Explicit, Size = 32)]
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

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
