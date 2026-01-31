namespace MouseFood;

public class RazerBatteryReader
{
    private readonly SynapseLogParser _synapseLogParser;

    public RazerBatteryReader(SynapseLogParser synapseLogParser)
    {
        _synapseLogParser = synapseLogParser;
    }

    public BatteryStatus? GetBatteryStatus()
    {
        return _synapseLogParser.GetBatteryFromLogs();
    }
}

public class BatteryStatus
{
    public int Percentage { get; set; }
    public bool IsCharging { get; set; }
    public DateTime LastUpdated { get; set; }
}
