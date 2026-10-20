
using SmartX.Api.Models;

namespace SmartX.Api.Services;

public class AnomalyDetectionService
{
    private readonly Dictionary<string, TelemetryReading> _previousReadings = new();
    private readonly List<TelemetryIncident> _incidents = new();

    // Prevents duplicate active alerts for the same problem.
    private readonly HashSet<string> _activeAlertKeys =
        new(StringComparer.OrdinalIgnoreCase);

    // Tracks devices currently considered offline.
    private readonly HashSet<string> _offlineDevices =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly object _lock = new();

    public void Analyze(TelemetryReading currentReading)
    {
        lock (_lock)
        {
            RemoveResolvedAlertKeys();

            string readingKey =
                $"{currentReading.DeviceId}:{currentReading.Metric}";

            // Receiving telemetry means this device is communicating again.
            _offlineDevices.Remove(currentReading.DeviceId);

            // Resolve existing offline incidents for this device.
            foreach (var incident in _incidents.Where(i =>
                i.DeviceId.Equals(
                    currentReading.DeviceId,
                    StringComparison.OrdinalIgnoreCase) &&
                i.Metric.Equals(
                    "Connection",
                    StringComparison.OrdinalIgnoreCase) &&
                !i.IsResolved))
            {
                incident.IsResolved = true;
            }

            RemoveResolvedAlertKeys();

            _previousReadings.TryGetValue(
                readingKey,
                out var previousReading);

            double difference = previousReading == null
                ? 0
                : currentReading.Value - previousReading.Value;

            if (currentReading.Metric.Equals(
                "Temperature",
                StringComparison.OrdinalIgnoreCase))
            {
                if (currentReading.Value > 70)
                {
                    AddIncident(
                        currentReading,
                        previousReading,
                        difference,
                        "Critical",
                        "Temperature spike detected.");
                }

                if (previousReading != null &&
                    Math.Abs(difference) > 30)
                {
                    AddIncident(
                        currentReading,
                        previousReading,
                        difference,
                        "Warning",
                        "Large temperature change detected.");
                }
            }

            if (currentReading.Metric.Equals(
                "Power",
                StringComparison.OrdinalIgnoreCase) &&
                currentReading.Value > 2000)
            {
                AddIncident(
                    currentReading,
                    previousReading,
                    difference,
                    "Critical",
                    "High power consumption detected.");
            }

            if (currentReading.Metric.Equals(
                "Moisture",
                StringComparison.OrdinalIgnoreCase) &&
                currentReading.Value < 20)
            {
                AddIncident(
                    currentReading,
                    previousReading,
                    difference,
                    "Critical",
                    "Low soil moisture detected.");
            }

            _previousReadings[readingKey] = currentReading;
        }
    }

    // Call this when the sensor monitoring service identifies an offline device.
    public void ReportDeviceOffline(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return;

        lock (_lock)
        {
            RemoveResolvedAlertKeys();

            // Only create one offline alert while the device remains offline.
            if (!_offlineDevices.Add(deviceId))
                return;

            string alertKey = CreateAlertKey(
                deviceId,
                "Connection",
                "Warning",
                "Device disconnected.");

            if (!_activeAlertKeys.Add(alertKey))
                return;

            _incidents.Add(new TelemetryIncident
            {
                DeviceId = deviceId,
                Metric = "Connection",
                Severity = "Warning",
                Message = "Device disconnected.",
                PreviousValue = 1,
                CurrentValue = 0,
                Difference = -1,
                Unit = "status",
                Timestamp = DateTime.UtcNow,
                IsResolved = false
            });
        }
    }

    private void AddIncident(
        TelemetryReading currentReading,
        TelemetryReading? previousReading,
        double difference,
        string severity,
        string message)
    {
        string alertKey = CreateAlertKey(
            currentReading.DeviceId,
            currentReading.Metric,
            severity,
            message);

        // Do not duplicate a problem that is already active.
        if (!_activeAlertKeys.Add(alertKey))
            return;

        var incident = new TelemetryIncident
        {
            DeviceId = currentReading.DeviceId,
            Metric = currentReading.Metric,
            Severity = severity,
            Message = message,
            PreviousValue = previousReading?.Value ?? currentReading.Value,
            CurrentValue = currentReading.Value,
            Difference = difference,
            Unit = currentReading.Unit,
            Timestamp = currentReading.Timestamp,
            IsResolved = false
        };

        _incidents.Add(incident);
    }

    private void RemoveResolvedAlertKeys()
    {
        var resolvedKeys = _incidents
            .Where(incident => incident.IsResolved)
            .Select(incident => CreateAlertKey(
                incident.DeviceId,
                incident.Metric,
                incident.Severity,
                incident.Message))
            .ToList();

        foreach (string key in resolvedKeys)
            _activeAlertKeys.Remove(key);
    }

    private static string CreateAlertKey(
        string deviceId,
        string metric,
        string severity,
        string message)
    {
        return $"{deviceId}|{metric}|{severity}|{message}";
    }

    public List<TelemetryIncident> GetIncidents()
    {
        lock (_lock)
        {
            return _incidents.ToList();
        }
    }
}
