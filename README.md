# AutoRun

Press a hotkey to hold down **Shift + W**; press it again to release. Press
another hotkey to quit.

- Toggle hold: `F6`
- Quit: `F7`

Edit `TOGGLE_HOTKEY`, `QUIT_HOTKEY`, or `HELD_KEYS` at the top of `main.py`
to change the keys.

## Requirements

- Windows
- Python 3.8+

## Run

```
pip install -r requirements.txt
python main.py
```

Run as Administrator if the hotkey doesn't work in a game running as admin
(the `keyboard` library needs matching privilege level to inject keys into
elevated windows).

## Build a standalone .exe (optional)

```
pip install pyinstaller
pyinstaller --onefile main.py
```

The executable will be in `dist/main.exe`.
