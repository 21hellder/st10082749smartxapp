
using SmartX.Api.Models;

namespace SmartX.Api.Services;

public class SensorStore
{
    // Primary storage and fast lookup indexes.
    private readonly Dictionary<string, Sensor> _sensorsById =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, Sensor> _sensorsByDeviceId =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, Sensor> _sensorsByMacAddress =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly object _lock = new();

    public void Add(Sensor sensor)
    {
        ArgumentNullException.ThrowIfNull(sensor);

        lock (_lock)
        {
            // Remove an existing registration for the same device or MAC.
            if (!string.IsNullOrWhiteSpace(sensor.DeviceId) &&
                _sensorsByDeviceId.TryGetValue(sensor.DeviceId, out var sameDevice))
            {
                RemoveSensor(sameDevice);
            }

            if (!string.IsNullOrWhiteSpace(sensor.MacAddress) &&
                _sensorsByMacAddress.TryGetValue(sensor.MacAddress, out var sameMac))
            {
                RemoveSensor(sameMac);
            }

            // Also avoid duplicate sensor IDs.
            if (!string.IsNullOrWhiteSpace(sensor.Id) &&
                _sensorsById.TryGetValue(sensor.Id, out var sameId))
            {
                RemoveSensor(sameId);
            }

            _sensorsById[sensor.Id] = sensor;

            if (!string.IsNullOrWhiteSpace(sensor.DeviceId))
            {
                _sensorsByDeviceId[sensor.DeviceId] = sensor;
            }

            if (!string.IsNullOrWhiteSpace(sensor.MacAddress))
            {
                _sensorsByMacAddress[sensor.MacAddress] = sensor;
            }
        }
    }

    public List<Sensor> GetAll()
    {
        lock (_lock)
        {
            return _sensorsById.Values.ToList();
        }
    }

    public Sensor? GetById(string id)
    {
        lock (_lock)
        {
            return _sensorsById.TryGetValue(id, out var sensor)
                ? sensor
                : null;
        }
    }

    public Sensor? GetByDeviceId(string deviceId)
    {
        lock (_lock)
        {
            return _sensorsByDeviceId.TryGetValue(deviceId, out var sensor)
                ? sensor
                : null;
        }
    }

    public Sensor? GetByMacAddress(string macAddress)
    {
        lock (_lock)
        {
            return _sensorsByMacAddress.TryGetValue(macAddress, out var sensor)
                ? sensor
                : null;
        }
    }

    public bool UpdateLastTelemetry(string deviceId)
    {
        lock (_lock)
        {
            if (!_sensorsByDeviceId.TryGetValue(deviceId, out var sensor))
            {
                return false;
            }

            sensor.LastTelemetryReceived = DateTime.UtcNow;
            sensor.IsOnline = true;

            return true;
        }
    }

    public void CheckOfflineSensors(int timeoutSeconds)
    {
        DateTime cutoff = DateTime.UtcNow.AddSeconds(-timeoutSeconds);

        lock (_lock)
        {
            foreach (var sensor in _sensorsById.Values)
            {
                if (sensor.LastTelemetryReceived < cutoff)
                {
                    sensor.IsOnline = false;
                }
            }
        }
    }

    private void RemoveSensor(Sensor sensor)
    {
        _sensorsById.Remove(sensor.Id);

        if (!string.IsNullOrWhiteSpace(sensor.DeviceId) &&
            _sensorsByDeviceId.TryGetValue(sensor.DeviceId, out var deviceMatch) &&
            ReferenceEquals(deviceMatch, sensor))
        {
            _sensorsByDeviceId.Remove(sensor.DeviceId);
        }

        if (!string.IsNullOrWhiteSpace(sensor.MacAddress) &&
            _sensorsByMacAddress.TryGetValue(sensor.MacAddress, out var macMatch) &&
            ReferenceEquals(macMatch, sensor))
        {
            _sensorsByMacAddress.Remove(sensor.MacAddress);
        }
    }
}
