
using Microsoft.AspNetCore.Builder;
using SmartX.Api.Models;
using SmartX.Api.Services;

namespace SmartX.Api.Endpoints;

public static class TelemetryEndpoints
{
    public static void MapTelemetryEndpoints(this WebApplication app)
    {
        var validationService =
            app.Services.GetRequiredService<TelemetryValidationService>();

        var telemetryStore =
            app.Services.GetRequiredService<TelemetryStore>();

        var batchBuffer =
            app.Services.GetRequiredService<TelemetryBatchBuffer>();

        var anomalyDetector =
            app.Services.GetRequiredService<AnomalyDetectionService>();

        var simulationService =
            app.Services.GetRequiredService<TelemetrySimulationService>();

        var processingService =
            app.Services.GetRequiredService<TelemetryProcessingService>();

        var sensorStore =
            app.Services.GetRequiredService<SensorStore>();

        var attachmentStore =
            app.Services.GetRequiredService<SensorAttachmentStore>();

        var hierarchyService =
            app.Services.GetRequiredService<SensorHierarchyService>();

        // Receive double telemetry and enqueue it for background processing.
        app.MapPost("/api/telemetry/double",
            (TelemetryPacket<double> packet) =>
            {
                if (!validationService.Validate(packet, out string error))
                {
                    return Results.BadRequest(new
                    {
                        success = false,
                        error
                    });
                }

                var reading = new TelemetryReading
                {
                    DeviceId = packet.DeviceId,
                    Metric = packet.Metric,
                    Value = packet.Value,
                    Unit = packet.Unit,
                    Timestamp = packet.Timestamp
                };

                processingService.Enqueue(reading);

                return Results.Accepted("/api/telemetry/history", new
                {
                    success = true,
                    message = "Double telemetry accepted for processing.",
                    packet
                });
            });

        // Receive integer telemetry and enqueue it for background processing.
        app.MapPost("/api/telemetry/int",
            (TelemetryPacket<int> packet) =>
            {
                if (!validationService.Validate(packet, out string error))
                {
                    return Results.BadRequest(new
                    {
                        success = false,
                        error
                    });
                }

                var reading = new TelemetryReading
                {
                    DeviceId = packet.DeviceId,
                    Metric = packet.Metric,
                    Value = packet.Value,
                    Unit = packet.Unit,
                    Timestamp = packet.Timestamp
                };

                processingService.Enqueue(reading);

                return Results.Accepted("/api/telemetry/history", new
                {
                    success = true,
                    message = "Integer telemetry accepted for processing.",
                    packet
                });
            });

        // Receive boolean telemetry and enqueue it for background processing.
        app.MapPost("/api/telemetry/bool",
            (TelemetryPacket<bool> packet) =>
            {
                if (!validationService.Validate(packet, out string error))
                {
                    return Results.BadRequest(new
                    {
                        success = false,
                        error
                    });
                }

                var reading = new TelemetryReading
                {
                    DeviceId = packet.DeviceId,
                    Metric = packet.Metric,
                    Value = packet.Value ? 1 : 0,
                    Unit = packet.Unit,
                    Timestamp = packet.Timestamp
                };

                processingService.Enqueue(reading);

                return Results.Accepted("/api/telemetry/history", new
                {
                    success = true,
                    message = "Boolean telemetry accepted for processing.",
                    packet
                });
            });

        // Retrieve processed telemetry history.
        app.MapGet("/api/telemetry/history", () =>
        {
            return Results.Ok(telemetryStore.GetAll());
        });

        // Retrieve buffered telemetry.
        app.MapGet("/api/telemetry/buffer", () =>
        {
            var readings = batchBuffer.TransferToList();

            return Results.Ok(new
            {
                success = true,
                count = readings.Count,
                readings
            });
        });

        // Generate simulated telemetry.
        // The simulator still uses its existing implementation for now.
        app.MapPost("/api/telemetry/simulate", (int count) =>
        {
            if (count <= 0)
            {
                return Results.BadRequest(new
                {
                    success = false,
                    error = "Count must be greater than zero."
                });
            }

            if (count > 10000)
            {
                return Results.BadRequest(new
                {
                    success = false,
                    error = "Maximum simulation size is 10000 readings."
                });
            }

            int generated = simulationService.Generate(count);

            return Results.Ok(new
            {
                success = true,
                message = "Mock IoT telemetry generated successfully.",
                generated
            });
        });

        // Retrieve detected incidents.
        app.MapGet("/api/incidents", () =>
        {
            var incidents = anomalyDetector.GetIncidents();

            return Results.Ok(new
            {
                success = true,
                count = incidents.Count,
                incidents
            });
        });

        // Retrieve sensor by device ID.
        app.MapGet("/api/sensors/{deviceId}", (string deviceId) =>
        {
            var sensor = sensorStore.GetByDeviceId(deviceId);

            if (sensor == null)
            {
                return Results.NotFound(new
                {
                    success = false,
                    error = "Sensor not found."
                });
            }

            return Results.Ok(new
            {
                success = true,
                sensor
            });
        });

        

        // Check offline sensors and create incidents for disconnected devices.
        app.MapPost("/api/sensors/check-offline", () =>
        {
            sensorStore.CheckOfflineSensors(30);

            var sensors = sensorStore.GetAll();

            foreach (var sensor in sensors)
            {
                if (!sensor.IsOnline)
                {
                    anomalyDetector.ReportDeviceOffline(sensor.DeviceId);
                }
            }

            var offlineDevices = sensors
                .Where(sensor => !sensor.IsOnline)
                .Select(sensor => sensor.DeviceId)
                .ToList();

            return Results.Ok(new
            {
                success = true,
                message = "Offline sensor check completed. Disconnected devices were reported to the incident monitor.",
                offlineCount = offlineDevices.Count,
                offlineDevices,
                sensors,
                incidents = anomalyDetector.GetIncidents()
            });
        });


        // Demonstrate recursive sensor hierarchy validation.
        app.MapGet("/api/sensors/hierarchy", () =>
        {
            var root = new SensorNode
            {
                DeviceId = "SMART-X-FARM",
                Location = "Main Farm"
            };

            foreach (var sensor in sensorStore.GetAll())
            {
                root.Children.Add(new SensorNode
                {
                    DeviceId = sensor.DeviceId,
                    Location = sensor.Location
                });
            }

            int totalNodes =
                hierarchyService.CountNodesRecursively(root);

            List<string> deviceIds =
                hierarchyService.GetDeviceIdsRecursively(root);

            bool hierarchyIsValid =
                hierarchyService.ValidateHierarchyRecursively(
                    root,
                    out List<string> validationErrors);

            return Results.Ok(new
            {
                success = true,
                message = "Sensor hierarchy processed using recursive validation.",
                hierarchyIsValid,
                totalNodes,
                deviceIds,
                validationErrors
            });
        });

        // Upload a sensor attachment.
        app.MapPost(
            "/api/sensors/{deviceId}/attachments",
            async (
                string deviceId,
                HttpRequest request,
                SensorStore sensorStore,
                SensorAttachmentStore attachmentStore) =>
            {
                var sensor = sensorStore.GetByDeviceId(deviceId);

                if (sensor == null)
                {
                    return Results.NotFound(new
                    {
                        success = false,
                        error = "Sensor not found."
                    });
                }

                if (!request.HasFormContentType)
                {
                    return Results.BadRequest(new
                    {
                        success = false,
                        error = "The request must use multipart/form-data."
                    });
                }

                var form = await request.ReadFormAsync();
                var file = form.Files.FirstOrDefault();

                if (file == null || file.Length == 0)
                {
                    return Results.BadRequest(new
                    {
                        success = false,
                        error = "A file is required."
                    });
                }

                const long maxFileSize = 10 * 1024 * 1024;

                if (file.Length > maxFileSize)
                {
                    return Results.BadRequest(new
                    {
                        success = false,
                        error = "File size cannot exceed 10 MB."
                    });
                }

                string extension =
                    Path.GetExtension(file.FileName).ToLowerInvariant();

                bool isPdf =
                    extension == ".pdf" ||
                    file.ContentType.Equals(
                        "application/pdf",
                        StringComparison.OrdinalIgnoreCase);

                string[] allowedExtensions =
                {
                    ".json",
                    ".txt",
                    ".log",
                    ".csv",
                    ".xml",
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".pdf"
                };

                if (!allowedExtensions.Contains(extension) && !isPdf)
                {
                    return Results.BadRequest(new
                    {
                        success = false,
                        error =
                            $"File type is not supported. Received extension: '{extension}', content type: '{file.ContentType}'."
                    });
                }

                string uploadsFolder = Path.Combine(
                    app.Environment.ContentRootPath,
                    "Uploads");

                Directory.CreateDirectory(uploadsFolder);

                string attachmentId = Guid.NewGuid().ToString();

                string storedExtension =
                    extension == ".pdf" || isPdf
                        ? ".pdf"
                        : extension;

                string storedFileName =
                    $"{attachmentId}{storedExtension}";

                string filePath = Path.Combine(
                    uploadsFolder,
                    storedFileName);

                await using (var stream = new FileStream(
                    filePath,
                    FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var attachment = new SensorAttachment
                {
                    Id = attachmentId,
                    SensorId = sensor.Id,
                    FileName = Path.GetFileName(file.FileName),
                    ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                        ? "application/octet-stream"
                        : file.ContentType,
                    FileSize = file.Length,
                    UploadedAt = DateTime.UtcNow,
                    FilePath = filePath
                };

                attachmentStore.Add(attachment);

                return Results.Ok(new
                {
                    success = true,
                    message = "File attachment uploaded successfully.",
                    attachment
                });
            })
            .DisableAntiforgery();

        // Retrieve attachments for a sensor.
        app.MapGet(
            "/api/sensors/{deviceId}/attachments",
            (string deviceId) =>
            {
                var sensor = sensorStore.GetByDeviceId(deviceId);

                if (sensor == null)
                {
                    return Results.NotFound(new
                    {
                        success = false,
                        error = "Sensor not found."
                    });
                }

                var attachments =
                    attachmentStore.GetBySensorId(sensor.Id);

                return Results.Ok(new
                {
                    success = true,
                    count = attachments.Count,
                    attachments
                });
            });

        // Download a sensor attachment.
        app.MapGet(
            "/api/sensors/attachments/{attachmentId}/download",
            (string attachmentId) =>
            {
                var attachment =
                    attachmentStore.GetById(attachmentId);

                if (attachment == null)
                {
                    return Results.NotFound(new
                    {
                        success = false,
                        error = "Attachment not found."
                    });
                }

                if (!File.Exists(attachment.FilePath))
                {
                    return Results.NotFound(new
                    {
                        success = false,
                        error = "Attachment file could not be found."
                    });
                }

                return Results.File(
                    attachment.FilePath,
                    attachment.ContentType,
                    attachment.FileName);
            });
    }
}
