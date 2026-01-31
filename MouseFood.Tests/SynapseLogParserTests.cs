using Microsoft.Extensions.Logging;
using Moq;

namespace MouseFood.Tests;

public class SynapseLogParserTests : IDisposable
{
    private readonly Mock<ILogger<SynapseLogParser>> _mockLogger;
    private readonly string _testLogDirectory;

    public SynapseLogParserTests()
    {
        _mockLogger = new Mock<ILogger<SynapseLogParser>>();
        _testLogDirectory = Path.Combine(Path.GetTempPath(), "MouseFoodTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testLogDirectory);
    }

    [Fact]
    public void GetBatteryFromLogs_WithValidLogFile_ReturnsCorrectBatteryStatus()
    {
        // Arrange
        var logContent = @"
[2026-01-31 10:00:00] Some log content
""powerStatus"":{""chargingStatus"":""NoCharge_BatteryFull"",""level"":87}
[2026-01-31 10:01:00] More log content
";
        var logFilePath = Path.Combine(_testLogDirectory, "systray_systrayv21.log");
        File.WriteAllText(logFilePath, logContent);

        var parser = CreateParser(_testLogDirectory);

        // Act
        var result = parser.GetBatteryFromLogs();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(87, result.Percentage);
        Assert.False(result.IsCharging);
    }

    [Fact]
    public void GetBatteryFromLogs_WithChargingStatus_ReturnsIsChargingTrue()
    {
        // Arrange
        var logContent = @"""powerStatus"":{""chargingStatus"":""Charging"",""level"":45}";
        var logFilePath = Path.Combine(_testLogDirectory, "systray_systrayv21.log");
        File.WriteAllText(logFilePath, logContent);

        var parser = CreateParser(_testLogDirectory);

        // Act
        var result = parser.GetBatteryFromLogs();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(45, result.Percentage);
        Assert.True(result.IsCharging);
    }

    [Fact]
    public void GetBatteryFromLogs_WithNoChargeStatus_ReturnsIsChargingFalse()
    {
        // Arrange
        var logContent = @"""powerStatus"":{""chargingStatus"":""NoCharge_BatteryFull"",""level"":100}";
        var logFilePath = Path.Combine(_testLogDirectory, "systray_systrayv21.log");
        File.WriteAllText(logFilePath, logContent);

        var parser = CreateParser(_testLogDirectory);

        // Act
        var result = parser.GetBatteryFromLogs();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.Percentage);
        Assert.False(result.IsCharging);
    }

    [Fact]
    public void GetBatteryFromLogs_WithMultiplePowerStatusEntries_ReturnsLatestEntry()
    {
        // Arrange
        var logContent = @"
""powerStatus"":{""chargingStatus"":""NoCharge"",""level"":30}
Some content in between
""powerStatus"":{""chargingStatus"":""Charging"",""level"":85}
";
        var logFilePath = Path.Combine(_testLogDirectory, "systray_systrayv21.log");
        File.WriteAllText(logFilePath, logContent);

        var parser = CreateParser(_testLogDirectory);

        // Act
        var result = parser.GetBatteryFromLogs();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(85, result.Percentage);
        Assert.True(result.IsCharging);
    }

    [Fact]
    public void GetBatteryFromLogs_WithNoLogFiles_ReturnsNull()
    {
        // Arrange
        var emptyDirectory = Path.Combine(_testLogDirectory, "empty");
        Directory.CreateDirectory(emptyDirectory);
        var parser = CreateParser(emptyDirectory);

        // Act
        var result = parser.GetBatteryFromLogs();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetBatteryFromLogs_WithNoPowerStatusInLogs_ReturnsNull()
    {
        // Arrange
        var logContent = @"
[2026-01-31 10:00:00] Some log content without power status
[2026-01-31 10:01:00] More log content
";
        var logFilePath = Path.Combine(_testLogDirectory, "systray_systrayv21.log");
        File.WriteAllText(logFilePath, logContent);

        var parser = CreateParser(_testLogDirectory);

        // Act
        var result = parser.GetBatteryFromLogs();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetBatteryFromLogs_WithMultipleLogFiles_ChecksMostRecent()
    {
        // Arrange
        var oldLogContent = @"""powerStatus"":{""chargingStatus"":""NoCharge"",""level"":50}";
        var newLogContent = @"""powerStatus"":{""chargingStatus"":""Charging"",""level"":90}";

        var oldLogPath = Path.Combine(_testLogDirectory, "systray_systrayv20.log");
        var newLogPath = Path.Combine(_testLogDirectory, "systray_systrayv21.log");

        File.WriteAllText(oldLogPath, oldLogContent);
        Thread.Sleep(100); // Ensure different timestamps
        File.WriteAllText(newLogPath, newLogContent);

        var parser = CreateParser(_testLogDirectory);

        // Act
        var result = parser.GetBatteryFromLogs();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(90, result.Percentage);
    }

    [Fact]
    public void GetBatteryFromLogs_WithInvalidBatteryLevel_ReturnsNull()
    {
        // Arrange
        var logContent = @"""powerStatus"":{""chargingStatus"":""NoCharge"",""level"":""invalid""}";
        var logFilePath = Path.Combine(_testLogDirectory, "systray_systrayv21.log");
        File.WriteAllText(logFilePath, logContent);

        var parser = CreateParser(_testLogDirectory);

        // Act
        var result = parser.GetBatteryFromLogs();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetBatteryFromLogs_WithBoundaryValues_ReturnsCorrectValues()
    {
        // Arrange - Test 0%
        var logContent = @"""powerStatus"":{""chargingStatus"":""NoCharge"",""level"":0}";
        var logFilePath = Path.Combine(_testLogDirectory, "systray_systrayv21.log");
        File.WriteAllText(logFilePath, logContent);

        var parser = CreateParser(_testLogDirectory);

        // Act
        var result = parser.GetBatteryFromLogs();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.Percentage);

        // Arrange - Test 100%
        logContent = @"""powerStatus"":{""chargingStatus"":""NoCharge_BatteryFull"",""level"":100}";
        File.WriteAllText(logFilePath, logContent);

        // Act
        result = parser.GetBatteryFromLogs();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.Percentage);
    }

    private SynapseLogParser CreateParser(string logDirectory)
    {
        // Inject test directory via constructor
        return new SynapseLogParser(_mockLogger.Object, logDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testLogDirectory))
        {
            Directory.Delete(_testLogDirectory, true);
        }
    }
}
