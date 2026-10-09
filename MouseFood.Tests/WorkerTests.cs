using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace MouseFood.Tests;

public class WorkerTests : IDisposable
{
    private readonly Mock<ILogger<Worker>> _mockLogger;
    private readonly Mock<IHostApplicationLifetime> _mockLifetime;
    private readonly Mock<RazerBatteryReader> _mockReader;
    private readonly Mock<ITrayIconManager> _mockTrayIcon;
    private readonly IOptions<MouseFoodSettings> _options;
    private readonly Worker _worker;

    public WorkerTests()
    {
        _mockLogger = new Mock<ILogger<Worker>>();
        _mockLifetime = new Mock<IHostApplicationLifetime>();
        _mockReader = new Mock<RazerBatteryReader>(
            new object[] { new Mock<SynapseLogParser>(
                new object[] { Mock.Of<ILogger<SynapseLogParser>>(), null! }).Object });
        _mockTrayIcon = new Mock<ITrayIconManager>();
        _options = Options.Create(new MouseFoodSettings { UpdateIntervalMinutes = 1 });

        _worker = new Worker(
            _mockLogger.Object,
            _mockLifetime.Object,
            _mockReader.Object,
            _mockTrayIcon.Object,
            _options);
    }

    [Fact]
    public async Task ExecuteAsync_PerformsInitialBatteryCheck()
    {
        // Arrange
        var status = new BatteryStatus { Percentage = 75, IsCharging = false, LastUpdated = DateTime.Now };
        _mockReader.Setup(r => r.GetBatteryStatus()).Returns(status);

        using var cts = new CancellationTokenSource();

        // Act - start, let ExecuteAsync run, then cancel
        await _worker.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();
        await _worker.StopAsync(CancellationToken.None);

        // Assert - initial check should have called GetBatteryStatus at least once
        _mockReader.Verify(r => r.GetBatteryStatus(), Times.AtLeastOnce);
        _mockTrayIcon.Verify(t => t.UpdateStatus(status), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WhenReaderThrows_UpdatesStatusWithNull()
    {
        // Arrange
        _mockReader.Setup(r => r.GetBatteryStatus()).Throws(new IOException("Log file locked"));

        using var cts = new CancellationTokenSource();

        // Act
        await _worker.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();
        await _worker.StopAsync(CancellationToken.None);

        // Assert - should pass null to tray icon on error
        _mockTrayIcon.Verify(t => t.UpdateStatus(null), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WhenReaderReturnsNull_UpdatesStatusWithNull()
    {
        // Arrange
        _mockReader.Setup(r => r.GetBatteryStatus()).Returns((BatteryStatus?)null);

        using var cts = new CancellationTokenSource();

        // Act
        await _worker.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();
        await _worker.StopAsync(CancellationToken.None);

        // Assert
        _mockTrayIcon.Verify(t => t.UpdateStatus(null), Times.AtLeastOnce);
    }

    [Fact]
    public void Constructor_WiresUpTrayIconEvents()
    {
        // Assert - events were subscribed to during construction
        _mockTrayIcon.VerifyAdd(t => t.RefreshRequested += It.IsAny<Action>(), Times.Once);
        _mockTrayIcon.VerifyAdd(t => t.ExitRequested += It.IsAny<Action>(), Times.Once);
    }

    [Fact]
    public void Dispose_UnsubscribesFromTrayIconEvents()
    {
        // Act
        _worker.Dispose();

        // Assert
        _mockTrayIcon.VerifyRemove(t => t.RefreshRequested -= It.IsAny<Action>(), Times.Once);
        _mockTrayIcon.VerifyRemove(t => t.ExitRequested -= It.IsAny<Action>(), Times.Once);
    }

    public void Dispose()
    {
        _worker.Dispose();
    }
}
