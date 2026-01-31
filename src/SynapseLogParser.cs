using System.Text.Json;
using System.Text.RegularExpressions;

namespace MouseFood;

public class SynapseLogParser
{
    private readonly ILogger<SynapseLogParser> _logger;
    private readonly string _logDirectory;
    private readonly string _logFilePattern = "systray_systrayv2*.log";

    public SynapseLogParser(ILogger<SynapseLogParser> logger, string? logDirectory = null)
    {
        _logger = logger;
        _logDirectory = logDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Razer", "RazerAppEngine", "User Data", "Logs");
    }

    public virtual BatteryStatus? GetBatteryFromLogs()
    {
        try
        {
            if (!Directory.Exists(_logDirectory))
            {
                _logger.LogWarning("Razer Synapse log directory not found: {Directory}", _logDirectory);
                return null;
            }

            // Get all systray log files, sorted by last write time (most recent first)
            var logFiles = Directory.GetFiles(_logDirectory, _logFilePattern)
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .ToList();

            if (logFiles.Count == 0)
            {
                _logger.LogWarning("No Synapse systray log files found");
                return null;
            }

            _logger.LogDebug("Found {Count} systray log file(s)", logFiles.Count);

            // Try to parse battery info from the most recent files
            foreach (var logFile in logFiles.Take(3)) // Check up to 3 most recent logs
            {
                var battery = ParseBatteryFromFile(logFile);
                if (battery != null)
                {
                    return battery;
                }
            }

            _logger.LogDebug("No battery data found in recent log files");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading Synapse logs");
            return null;
        }
    }

    private BatteryStatus? ParseBatteryFromFile(string filePath)
    {
        try
        {
            _logger.LogDebug("Parsing log file: {File}", Path.GetFileName(filePath));

            // Read the last portion of the file (last 100KB should be enough)
            const int maxBytes = 100 * 1024;
            string content;

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var fileLength = stream.Length;
                var startPos = Math.Max(0, fileLength - maxBytes);
                stream.Seek(startPos, SeekOrigin.Begin);

                using (var reader = new StreamReader(stream))
                {
                    content = reader.ReadToEnd();
                }
            }

            // Search for powerStatus in the log
            // Format: "powerStatus":{"chargingStatus":"NoCharge_BatteryFull","level":87}
            var matches = Regex.Matches(content, @"""powerStatus"":\s*\{[^}]+\}", RegexOptions.RightToLeft);

            if (matches.Count == 0)
            {
                _logger.LogDebug("No powerStatus found in {File}", Path.GetFileName(filePath));
                return null;
            }

            // Get the most recent powerStatus entry (first match when searching from the end)
            var powerStatusJson = matches[0].Value;
            _logger.LogDebug("Found powerStatus: {Json}", powerStatusJson);

            // Parse the JSON fragment
            var match = Regex.Match(powerStatusJson, @"""level""\s*:\s*(\d+)");
            if (!match.Success)
            {
                _logger.LogWarning("Could not extract battery level from: {Json}", powerStatusJson);
                return null;
            }

            int batteryLevel = int.Parse(match.Groups[1].Value);

            // Parse charging status
            var chargingMatch = Regex.Match(powerStatusJson, @"""chargingStatus""\s*:\s*""([^""]+)""");
            string chargingStatus = chargingMatch.Success ? chargingMatch.Groups[1].Value : "Unknown";

            // Determine if charging
            // Known statuses: "NoCharge_BatteryFull", "Charging", "Discharging", etc.
            bool isCharging = chargingStatus.Contains("Charging", StringComparison.OrdinalIgnoreCase) &&
                             !chargingStatus.Contains("NoCharge", StringComparison.OrdinalIgnoreCase);

            _logger.LogInformation("Battery from Synapse logs: {Level}%, Status: {Status}, Charging: {Charging}",
                batteryLevel, chargingStatus, isCharging);

            return new BatteryStatus
            {
                Percentage = batteryLevel,
                IsCharging = isCharging,
                LastUpdated = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error parsing {File}", Path.GetFileName(filePath));
            return null;
        }
    }
}
