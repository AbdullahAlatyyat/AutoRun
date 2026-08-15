"""AutoRun - hold Shift+W with a single hotkey press, toggled off with another press."""

import sys

import keyboard

TOGGLE_HOTKEY = "f6"
QUIT_HOTKEY = "f7"
HELD_KEYS = ("shift", "w")

holding = False


def toggle():
    global holding
    if holding:
        for key in HELD_KEYS:
            keyboard.release(key)
        holding = False
        print("AutoRun: OFF")
    else:
        for key in HELD_KEYS:
            keyboard.press(key)
        holding = True
        print("AutoRun: ON  (holding Shift+W)")


def quit_app():
    if holding:
        for key in HELD_KEYS:
            keyboard.release(key)
    print("\nAutoRun: exiting")
    sys.exit(0)


def main():
    print("AutoRun is running.")
    print(f"  Press [{TOGGLE_HOTKEY.upper()}] to toggle holding Shift+W.")
    print(f"  Press [{QUIT_HOTKEY.upper()}] to quit.")

    keyboard.add_hotkey(TOGGLE_HOTKEY, toggle)
    keyboard.add_hotkey(QUIT_HOTKEY, quit_app)

    keyboard.wait()


if __name__ == "__main__":
    main()
