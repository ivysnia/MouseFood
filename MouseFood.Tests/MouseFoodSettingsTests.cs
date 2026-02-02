namespace MouseFood.Tests;

public class MouseFoodSettingsTests
{
    [Fact]
    public void DefaultUpdateIntervalMinutes_IsFive()
    {
        var settings = new MouseFoodSettings();

        Assert.Equal(5, settings.UpdateIntervalMinutes);
    }

    [Fact]
    public void UpdateIntervalMinutes_CanBeOverridden()
    {
        var settings = new MouseFoodSettings { UpdateIntervalMinutes = 15 };

        Assert.Equal(15, settings.UpdateIntervalMinutes);
    }
}
