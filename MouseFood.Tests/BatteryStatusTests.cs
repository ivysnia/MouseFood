namespace MouseFood.Tests;

public class BatteryStatusTests
{
    [Fact]
    public void BatteryStatus_CanSetAndGetPercentage()
    {
        // Arrange
        var status = new BatteryStatus();

        // Act
        status.Percentage = 85;

        // Assert
        Assert.Equal(85, status.Percentage);
    }

    [Fact]
    public void BatteryStatus_CanSetAndGetIsCharging()
    {
        // Arrange
        var status = new BatteryStatus();

        // Act
        status.IsCharging = true;

        // Assert
        Assert.True(status.IsCharging);
    }

    [Fact]
    public void BatteryStatus_CanSetAndGetLastUpdated()
    {
        // Arrange
        var status = new BatteryStatus();
        var now = DateTime.Now;

        // Act
        status.LastUpdated = now;

        // Assert
        Assert.Equal(now, status.LastUpdated);
    }

    [Fact]
    public void BatteryStatus_InitializerSyntax_WorksCorrectly()
    {
        // Arrange & Act
        var now = DateTime.Now;
        var status = new BatteryStatus
        {
            Percentage = 50,
            IsCharging = true,
            LastUpdated = now
        };

        // Assert
        Assert.Equal(50, status.Percentage);
        Assert.True(status.IsCharging);
        Assert.Equal(now, status.LastUpdated);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(20)]
    [InlineData(50)]
    [InlineData(100)]
    public void BatteryStatus_AcceptsValidPercentageValues(int percentage)
    {
        // Arrange
        var status = new BatteryStatus();

        // Act
        status.Percentage = percentage;

        // Assert
        Assert.Equal(percentage, status.Percentage);
    }
}
