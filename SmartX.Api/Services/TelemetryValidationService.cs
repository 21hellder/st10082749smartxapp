using SmartX.Api.Models;

namespace SmartX.Api.Services;

public class TelemetryValidationService
{
    public bool Validate<T>(TelemetryPacket<T> packet, out string error)
    {
        if (string.IsNullOrWhiteSpace(packet.DeviceId))
        {
            error = "Device ID is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(packet.SensorId))
        {
            error = "Sensor ID is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(packet.Metric))
        {
            error = "Metric is required.";
            return false;
        }

        if (packet.Value is null)
        {
            error = "Telemetry value is required.";
            return false;
        }

        if (packet.Timestamp > DateTime.UtcNow.AddMinutes(1))
        {
            error = "Telemetry timestamp cannot be in the future.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}