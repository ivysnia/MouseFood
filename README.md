# MouseFood

A lightweight Windows tray app that shows your Razer wireless mouse battery status. Reads from Razer Synapse 4 log files, so it works alongside Synapse without conflicts.

**Requires:** Razer Synapse 4 running with a connected wireless mouse.

## Features

- Battery percentage in the system tray with color-coded icon (green/orange/red/charging)
- Low battery notification at 20%
- Windows 11 dark mode support
- **Start with Windows** option in the context menu

## Build & Run

```bash
dotnet run --project src/MouseFood.csproj
```

Or build a release:

```bash
dotnet build -c Release
```

Right-click the tray icon for options: **Refresh Now**, **Start with Windows**, and **Exit**.

## Configuration

Edit `src/appsettings.json` to change the update interval (default 10 minutes):

```json
{
  "MouseFoodSettings": {
    "UpdateIntervalMinutes": 10
  }
}
```

## Troubleshooting

If the icon shows "?" or no battery data:

1. Make sure Razer Synapse 4 is running and shows battery status
2. Check that logs exist at `%LOCALAPPDATA%\Razer\RazerAppEngine\User Data\Logs\`
3. Set log level to `"Debug"` in `appsettings.json` for more detail

## Credits

Inspired by [razer-taskbar](https://github.com/FICTURE7/razer-taskbar) for the Synapse log parsing approach.
