# AutoRun

Press a hotkey to hold down **Shift + W**; press it again to release. Press
another hotkey to quit.

- Toggle hold: `F6`
- Quit: `F7`

Edit `ToggleKey`, `QuitKey`, or the held keys (`VK_SHIFT`, `VK_W`) in
`Program.cs` to change them.

## Requirements

- Windows
- .NET 8 SDK

## Run

```
dotnet run
```

Run as Administrator if the hotkey doesn't work in a game running as admin
(Windows blocks lower-privilege processes from sending input to
higher-privilege windows).

## Build a standalone .exe

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The executable will be in `bin/Release/net8.0-windows/win-x64/publish/AutoRun.exe`.
