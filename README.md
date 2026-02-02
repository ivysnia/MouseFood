# MouseFood

A lightweight Windows tray app that shows your Razer wireless mouse battery status. Reads from Razer Synapse 4 log files, so it works alongside Synapse without conflicts.

**Requires:** Razer Synapse 4 running with a connected wireless mouse.

## Features

- Battery percentage in the system tray with color-coded icon (green/orange/red/charging)
- Low battery notification at 20%
- Windows 11 dark mode support
- **Start with Windows** option in the context menu
- **Show Log** viewer with live updates, copy and save-to-file support
- Single-instance enforcement (only one copy runs at a time)

## Build & Run

```bash
dotnet run --project src/MouseFood.csproj
```

To publish a self-contained single-file executable (~30MB, no .NET runtime required):

```bash
dotnet publish src/MouseFood.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o Dist
```

This produces `Dist/MouseFood.exe` which can be copied anywhere and run standalone.

Right-click the tray icon for options: **Refresh Now**, **Show Log**, **Start with Windows**, **Taskbar Settings...**, and **Exit**.

## Configuration

Edit `src/appsettings.json` to change the update interval (default 5 minutes):

```json
{
  "MouseFoodSettings": {
    "UpdateIntervalMinutes": 5
  }
}
```

## Troubleshooting

If the icon shows "?" or no battery data:

1. Make sure Razer Synapse 4 is running and shows battery status
2. Check that logs exist at `%LOCALAPPDATA%\Razer\RazerAppEngine\User Data\Logs\`
3. Right-click the tray icon → **Show Log** to view live diagnostic output
4. Set log level to `"Debug"` in `appsettings.json` for more detail

## Credits

Inspired by [razer-taskbar](https://github.com/FICTURE7/razer-taskbar) for the Synapse log parsing approach.
