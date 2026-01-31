# MouseFood - Razer Mouse Battery Monitor

A lightweight .NET application that displays your Razer Naga Pro 2 (and other Razer wireless mice) battery status in the Windows system tray.

## How It Works

MouseFood reads battery information from **Razer Synapse 4 log files**, so it works seamlessly alongside Synapse without requiring direct USB access. The app monitors the systray logs where Synapse records device status updates including battery level and charging state.

**Requirements:**
- Razer Synapse 4 must be running
- Works with any Razer device that reports battery status in Synapse logs

## Features

- Real-time battery percentage display in system tray icon
- Configurable update interval (default: 10 minutes)
- Visual battery indicator with color coding:
  - Green: >50%
  - Orange: 20-50%
  - Red: <20%
  - Light green when charging
- Right-click context menu with:
  - **Refresh Now** - manually update battery status
  - **Exit** - close the application
- **Low battery notification** - shows Windows notification when battery drops to 20%
- **Windows 11 dark mode support** - context menu automatically matches system theme
- Runs seamlessly alongside Razer Synapse - no conflicts or USB access issues

## Configuration

Edit [src/appsettings.json](src/appsettings.json) to customize the update interval:

```json
{
  "MouseFoodSettings": {
    "UpdateIntervalMinutes": 10
  }
}
```

The application automatically detects your Razer device from Synapse logs - no device IDs or configuration needed

## Project Structure

```
MouseFood/
├── src/                           # Main application
│   ├── Program.cs                 # Entry point and host setup
│   ├── Worker.cs                  # Background service for periodic updates
│   ├── TrayIconManager.cs         # System tray icon and dark mode support
│   ├── SynapseLogParser.cs        # Parses Synapse log files
│   ├── RazerBatteryReader.cs      # Battery status reader
│   ├── BatteryStatus.cs           # Battery data model
│   ├── MouseFoodSettings.cs       # Configuration model
│   ├── appsettings.json           # Application settings
│   └── MouseFood.csproj           # Project file
├── MouseFood.Tests/               # Unit tests
│   ├── SynapseLogParserTests.cs   # Parser tests (9 tests)
│   ├── RazerBatteryReaderTests.cs # Reader tests (2 tests)
│   ├── BatteryStatusTests.cs      # Model tests (8 tests)
│   └── MouseFood.Tests.csproj     # Test project file
└── MouseFood.sln                  # Solution file
```

## Running the Application

### Development Mode
```bash
dotnet run --project src/MouseFood.csproj
```

### Release Build
```bash
dotnet build -c Release
```

The executable will be in: `bin\Release\net10.0-windows\MouseFood.exe`

### Running Tests
```bash
dotnet test
```

All 19 unit tests should pass, covering log parsing, battery reading, and data models.

### Running at Startup

To run MouseFood at Windows startup:

1. Press `Win+R` and type `shell:startup`
2. Create a shortcut to `MouseFood.exe` in the startup folder
3. (Optional) Right-click shortcut > Properties > Run: Minimized

## Troubleshooting

### No Battery Status Showing

If you see "No battery data available" or the icon shows "?":

1. **Make sure Razer Synapse 4 is running** - the app reads from Synapse logs
2. **Check your mouse is connected** - via USB dongle or Bluetooth
3. **Verify Synapse shows battery status** - open Synapse and check if it displays battery level
4. **Check log location** - the app looks for logs at:
   ```
   %LOCALAPPDATA%\Razer\RazerAppEngine\User Data\Logs\systray_systrayv2*.log
   ```
5. **Enable debug logging** - edit `src/appsettings.json` and set log level to "Debug" to see parsing details

### Battery Status Not Updating

If the battery percentage seems stale:

1. **Click "Refresh Now"** from the context menu to manually update
2. **Check the update interval** - default is 10 minutes, configurable in `appsettings.json`
3. **Synapse must be logging battery updates** - ensure your mouse is actively reporting to Synapse

### Context Menu Not Appearing

If right-click doesn't show the menu:

1. **Check Windows notification area settings** - ensure MouseFood icon is visible
2. **Try left-clicking first** to focus the icon, then right-click
3. **Restart the application** - the tray icon should reinitialize

### Dark Mode Not Applied

If the context menu appears in light theme on Windows 11 dark mode:

1. The app reads registry: `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme`
2. Try toggling Windows theme: Settings > Personalization > Colors > Choose your mode
3. Restart the application after changing theme

## Technical Details

### Architecture

- **Log Parsing Approach**: Reads Razer Synapse 4 log files instead of direct USB/HID access
  - Avoids conflicts with Synapse's exclusive device lock
  - Works with any Razer device supported by Synapse
  - Parses `powerStatus` JSON fragments from systray logs

- **Background Service**: Uses .NET Generic Host with Worker Service pattern
  - Periodic updates via configurable timer
  - Manual refresh support via `ManualResetEventSlim`

- **System Tray Integration**: Windows Forms `NotifyIcon` on main UI thread
  - Custom `ToolStripProfessionalRenderer` for Windows 11 dark mode
  - Rounded corners and smooth anti-aliased rendering
  - Dynamic icon generation with battery level and charging indicator

- **Dark Mode Support**:
  - Detects Windows theme via registry
  - Custom color table with dark background (#1E1E1E) and borders (#141414)
  - Rounded border rendering using `GraphicsPath` and `Region` clipping

### Key Components

- **SynapseLogParser**: Scans Synapse log directory, reads last 100KB of recent logs, uses regex to extract battery JSON
- **TrayIconManager**: Manages system tray icon, context menu, dark mode rendering, and notifications
- **Worker**: Background service that polls battery status at configured intervals
- **RazerBatteryReader**: Simple wrapper that delegates to SynapseLogParser

### Testing

- **Unit Test Coverage**: 19 tests across 3 test suites
  - Uses xUnit testing framework
  - Moq library for mocking dependencies
  - Isolated test log directories for parser tests
  - Mock-based tests for reader component

## Logging

The application uses Microsoft.Extensions.Logging with the following log levels:

- **Info**: Battery status updates, application lifecycle events
- **Debug**: Log file discovery, parsing details, powerStatus JSON fragments
- **Error**: File read errors, parsing failures

To enable debug logging, edit `src/appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "MouseFood": "Debug"
    }
  }
}
```

## Credits

Built with:
- **.NET 10.0** - Modern .NET platform
- **Windows Forms** - System tray integration and context menus
- **Microsoft.Extensions.Hosting** - Generic Host and dependency injection
- **xUnit** - Unit testing framework
- **Moq** - Mocking library for tests

Inspired by the [razer-taskbar](https://github.com/FICTURE7/razer-taskbar) project for the Synapse log parsing approach.
