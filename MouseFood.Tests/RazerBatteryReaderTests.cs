using Moq;

namespace MouseFood.Tests;

public class RazerBatteryReaderTests
{
    [Fact]
    public void GetBatteryStatus_CallsSynapseLogParser()
    {
        // Arrange
        var expectedStatus = new BatteryStatus
        {
            Percentage = 75,
            IsCharging = false,
            LastUpdated = DateTime.Now
        };

        var mockParser = new Mock<SynapseLogParser>(Mock.Of<Microsoft.Extensions.Logging.ILogger<SynapseLogParser>>(), (string?)null);
        mockParser.Setup(p => p.GetBatteryFromLogs()).Returns(expectedStatus);

        var reader = new RazerBatteryReader(mockParser.Object);

        // Act
        var result = reader.GetBatteryStatus();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedStatus.Percentage, result.Percentage);
        Assert.Equal(expectedStatus.IsCharging, result.IsCharging);
        mockParser.Verify(p => p.GetBatteryFromLogs(), Times.Once);
    }

    [Fact]
    public void GetBatteryStatus_WhenParserReturnsNull_ReturnsNull()
    {
        // Arrange
        var mockParser = new Mock<SynapseLogParser>(Mock.Of<Microsoft.Extensions.Logging.ILogger<SynapseLogParser>>(), (string?)null);
        mockParser.Setup(p => p.GetBatteryFromLogs()).Returns((BatteryStatus?)null);

        var reader = new RazerBatteryReader(mockParser.Object);

        // Act
        var result = reader.GetBatteryStatus();

        // Assert
        Assert.Null(result);
    }
}
