using Microsoft.Extensions.Options;

namespace MouseFood;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly RazerBatteryReader _batteryReader;
    private readonly TrayIconManager _trayIcon;
    private readonly MouseFoodSettings _settings;
    private readonly ManualResetEventSlim _refreshEvent = new(false);

    public Worker(
        ILogger<Worker> logger,
        IHostApplicationLifetime applicationLifetime,
        RazerBatteryReader batteryReader,
        TrayIconManager trayIcon,
        IOptions<MouseFoodSettings> settings)
    {
        _logger = logger;
        _applicationLifetime = applicationLifetime;
        _batteryReader = batteryReader;
        _trayIcon = trayIcon;
        _settings = settings.Value;

        // Wire up tray icon events
        _trayIcon.ExitRequested += OnExitRequested;
        _trayIcon.RefreshRequested += OnRefreshRequested;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MouseFood started. Update interval: {Minutes} minutes",
            _settings.UpdateIntervalMinutes);

        // Initial battery check
        UpdateBatteryStatus();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for either the configured interval or manual refresh
                var delayMilliseconds = _settings.UpdateIntervalMinutes * 60 * 1000;

                if (_refreshEvent.Wait(delayMilliseconds, stoppingToken))
                {
                    // Manual refresh requested
                    _refreshEvent.Reset();
                    _logger.LogInformation("Manual refresh requested");
                }

                UpdateBatteryStatus();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in worker loop");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("MouseFood stopped");
    }

    private void UpdateBatteryStatus()
    {
        try
        {
            var status = _batteryReader.GetBatteryStatus();
            _trayIcon.UpdateStatus(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update battery status");
            _trayIcon.UpdateStatus(null);
        }
    }

    private void OnRefreshRequested()
    {
        _refreshEvent.Set();
    }

    private void OnExitRequested()
    {
        _logger.LogInformation("Exit requested from tray icon");
        _applicationLifetime.StopApplication();

        // Exit the Windows Forms message loop
        System.Windows.Forms.Application.Exit();
    }

    public override void Dispose()
    {
        _trayIcon.ExitRequested -= OnExitRequested;
        _trayIcon.RefreshRequested -= OnRefreshRequested;
        _refreshEvent.Dispose();
        base.Dispose();
    }
}
