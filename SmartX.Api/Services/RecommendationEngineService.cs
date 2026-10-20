using SmartX.Api.Models;

namespace SmartX.Api.Services;

public sealed class RecommendationEngineService
{
    private readonly TelemetryStore _telemetryStore;
    private readonly DeviceCommandService _commandService;
    private readonly SensorStore _sensorStore;

    private readonly Dictionary<string, int> _searchPatterns =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly object _lock = new();

    public RecommendationEngineService(
        TelemetryStore telemetryStore,
        DeviceCommandService commandService,
        SensorStore sensorStore)
    {
        _telemetryStore = telemetryStore;
        _commandService = commandService;
        _sensorStore = sensorStore;
    }

    public void RecordSearch(string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return;

        string term = searchTerm.Trim();

        if (term.Length < 3)
            return;

        lock (_lock)
        {
            if (_searchPatterns.ContainsKey(term))
                _searchPatterns[term]++;
            else
                _searchPatterns[term] = 1;
        }
    }

    public List<Recommendation> GetRecommendations()
    {
        var recommendations = new List<Recommendation>();

        var readings = _telemetryStore.GetAll();
        var commands = _commandService.GetHistory();
        var sensors = _sensorStore.GetAll();

        // Analyse the latest reading for each device and metric.
        var latestReadings = readings
            .GroupBy(
                r => $"{r.DeviceId}|{r.Metric}",
                StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(r => r.Timestamp).First())
            .ToList();

        foreach (var reading in latestReadings)
        {
            string metric = reading.Metric.ToLowerInvariant();

            if (metric == "temperature" && reading.Value > 70)
            {
                AddRecommendation(
                    recommendations,
                    "Critical",
                    "High temperature detected",
                    $"Temperature is {reading.Value} {reading.Unit}. Inspect the device and cooling system.",
                    reading.DeviceId,
                    100);
            }
            else if (metric == "power" && reading.Value > 2000)
            {
                AddRecommendation(
                    recommendations,
                    "Critical",
                    "High power consumption",
                    $"Power reading is {reading.Value} {reading.Unit}. Investigate possible overload.",
                    reading.DeviceId,
                    100);
            }
            else if (metric == "moisture" && reading.Value < 20)
            {
                AddRecommendation(
                    recommendations,
                    "Critical",
                    "Low moisture detected",
                    $"Moisture reading is {reading.Value} {reading.Unit}. Inspect irrigation requirements.",
                    reading.DeviceId,
                    100);
            }
        }

        // Identify registered sensors reported as offline.
        foreach (var sensor in sensors.Where(s => !s.IsOnline))
        {
            AddRecommendation(
                recommendations,
                "Warning",
                "Device reported offline",
                $"Check the connection and last telemetry for device {sensor.DeviceId}.",
                sensor.DeviceId,
                80);
        }

        // Analyse recent manual commands, ignoring commands marked undone.
        var activeCommands = commands
            .Where(c => !c.IsUndone)
            .OrderByDescending(c => c.Timestamp)
            .ToList();

        foreach (var group in activeCommands.GroupBy(
            c => $"{c.DeviceId}|{c.Component}",
            StringComparer.OrdinalIgnoreCase))
        {
            var recent = group.Take(10).ToList();

            if (recent.Count < 2)
                continue;

            var latest = recent[0];

            // Opposing actions on the same component may indicate
            // repeated manual intervention.
            bool conflictingActions = recent
                .Take(5)
                .Select(c => c.Action)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() > 1;

            if (conflictingActions)
            {
                AddRecommendation(
                    recommendations,
                    "Warning",
                    "Conflicting manual commands",
                    $"{latest.Component} on {latest.DeviceId} has received both ON and OFF commands recently. Review the command history and intended state.",
                    latest.DeviceId,
                    70);
            }

            // Repeated commands can indicate that a device or process
            // needs investigation.
            var repeated = recent
                .GroupBy(c => c.Action, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault(g => g.Count() >= 3);

            if (repeated is not null)
            {
                AddRecommendation(
                    recommendations,
                    "Info",
                    "Repeated command pattern",
                    $"{latest.DeviceId} received {repeated.Count()} {latest.Component} {repeated.Key} commands. Review whether repeated overrides are necessary.",
                    latest.DeviceId,
                    50);
            }
        }

        // Analyse repeated searches recorded by the dashboard.
        KeyValuePair<string, int>[] searches;

        lock (_lock)
        {
            searches = _searchPatterns
                .OrderByDescending(p => p.Value)
                .ToArray();
        }

        foreach (var search in searches.Where(s => s.Value >= 2).Take(5))
        {
            AddRecommendation(
                recommendations,
                "Info",
                "Repeated search pattern",
                $"The search term '{search.Key}' has been used {search.Value} times. Review matching devices for recurring problems.",
                null,
                30);
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add(new Recommendation(
                "Info",
                "No current issues detected",
                "No recommendation rules were triggered by the available telemetry, device status, command history, or search patterns.",
                null,
                10));
        }

        return recommendations
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.DeviceId)
            .ToList();
    }

    private static void AddRecommendation(
        List<Recommendation> recommendations,
        string priority,
        string title,
        string message,
        string? deviceId,
        int score)
    {
        // Avoid showing the same rule repeatedly for one device.
        bool alreadyExists = recommendations.Any(r =>
            r.Title.Equals(title, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(
                r.DeviceId,
                deviceId,
                StringComparison.OrdinalIgnoreCase));

        if (!alreadyExists)
        {
            recommendations.Add(new Recommendation(
                priority,
                title,
                message,
                deviceId,
                score));
        }
    }

    public sealed record Recommendation(
        string Priority,
        string Title,
        string Message,
        string? DeviceId,
        int Score);
}