
using SmartX.Api.Models;

namespace SmartX.Api.Services;

public class TelemetrySimulationService
{
    private readonly TelemetryProcessingService _processingService;
    private readonly Random _random = new();

    public TelemetrySimulationService(
        TelemetryProcessingService processingService)
    {
        _processingService = processingService;
    }

    public int Generate(int count)
    {
        if (count < 1)
        {
            return 0;
        }

        int generated = 0;

        for (int i = 0; i < count; i++)
        {
            string deviceId = $"ESP32-{_random.Next(1, 101):D3}";

            int metricChoice = _random.Next(3);

            TelemetryReading reading;

            switch (metricChoice)
            {
                case 0:
                    {
                        double temperature = _random.Next(15, 36);

                        // Occasionally generate a critical temperature.
                        if (_random.Next(1, 11) == 1)
                        {
                            temperature = _random.Next(71, 91);
                        }

                        reading = new TelemetryReading
                        {
                            DeviceId = deviceId,
                            Metric = "Temperature",
                            Value = temperature,
                            Unit = "°C",
                            Timestamp = DateTime.UtcNow
                        };

                        break;
                    }

                case 1:
                    {
                        double power = _random.Next(100, 1501);

                        // Occasionally generate critical power consumption.
                        if (_random.Next(1, 11) == 1)
                        {
                            power = _random.Next(2001, 3001);
                        }

                        reading = new TelemetryReading
                        {
                            DeviceId = deviceId,
                            Metric = "Power",
                            Value = power,
                            Unit = "W",
                            Timestamp = DateTime.UtcNow
                        };

                        break;
                    }

                default:
                    {
                        reading = new TelemetryReading
                        {
                            DeviceId = deviceId,
                            Metric = "ValveState",
                            Value = _random.Next(0, 2),
                            Unit = "state",
                            Timestamp = DateTime.UtcNow
                        };

                        break;
                    }
            }

            // All simulated readings now use the same processing pipeline
            // as telemetry received through the API.
            _processingService.Enqueue(reading);

            generated++;
        }

        return generated;
    }
}
