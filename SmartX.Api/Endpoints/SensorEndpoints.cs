using SmartX.Api.Models;
using SmartX.Api.Services;

namespace SmartX.Api.Endpoints;

public static class SensorEndpoints
{
    public static void MapSensorEndpoints(
        this WebApplication app,
        SensorStore sensorStore)
    {
        app.MapGet("/api/sensors", () =>
        {
            var sensors = sensorStore.GetAll();

            return Results.Ok(new
            {
                success = true,
                sensors
            });
        });

        app.MapPost("/api/sensors", (Sensor sensor) =>
        {
            if (string.IsNullOrWhiteSpace(sensor.DeviceId))
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = "Device ID is required."
                });
            }

            if (string.IsNullOrWhiteSpace(sensor.MacAddress))
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = "MAC address is required."
                });
            }

            if (string.IsNullOrWhiteSpace(sensor.Location))
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = "Deployment location is required."
                });
            }

            if (string.IsNullOrWhiteSpace(sensor.Category))
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = "Sensor category is required."
                });
            }

            if (sensorStore.GetByDeviceId(sensor.DeviceId) != null)
            {
                return Results.Conflict(new
                {
                    success = false,
                    message = "A sensor with this Device ID already exists."
                });
            }

            if (sensorStore.GetByMacAddress(sensor.MacAddress) != null)
            {
                return Results.Conflict(new
                {
                    success = false,
                    message = "A sensor with this MAC address already exists."
                });
            }

            sensor.Id = Guid.NewGuid().ToString();
            sensor.IsOnline = true;
            sensor.LastTelemetryReceived = DateTime.UtcNow;

            sensorStore.Add(sensor);

            return Results.Ok(new
            {
                success = true,
                message = "Sensor registered successfully.",
                sensor
            });
        });
    }
}