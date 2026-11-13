using Microsoft.AspNetCore.SignalR;
using SmartX.Api.Hubs;
using SmartX.Api.Models;

namespace SmartX.Api.Services;

public sealed class TelemetryProcessingService : BackgroundService
{
    private readonly Queue<TelemetryReading> _normalQueue = new();
    private readonly PriorityQueue<TelemetryReading, int> _priorityQueue = new();
    private readonly object _queueLock = new();
    private readonly SemaphoreSlim _signal = new(0);

    private readonly TelemetryStore _store;
    private readonly TelemetryBatchBuffer _buffer;
    private readonly AnomalyDetectionService _anomalyDetector;
    private readonly SensorStore _sensorStore;
    private readonly IHubContext<TelemetryHub> _hubContext;
    private readonly ILogger<TelemetryProcessingService> _logger;
    private readonly TelemetryRangeService _rangeService;

    public TelemetryProcessingService(
        TelemetryStore store,
        TelemetryBatchBuffer buffer,
        AnomalyDetectionService anomalyDetector,
        SensorStore sensorStore,
        IHubContext<TelemetryHub> hubContext,
        ILogger<TelemetryProcessingService> logger,
        TelemetryRangeService rangeService)
    {
        _store = store;
        _buffer = buffer;
        _anomalyDetector = anomalyDetector;
        _sensorStore = sensorStore;
        _hubContext = hubContext;
        _logger = logger;
        _rangeService = rangeService;
    }

    public void Enqueue(TelemetryReading reading)
    {
        lock (_queueLock)
        {
            if (IsCritical(reading))
            {
                _priorityQueue.Enqueue(reading, 0);
            }
            else
            {
                _normalQueue.Enqueue(reading);
            }
        }

        _signal.Release();
    }

    private static bool IsCritical(TelemetryReading reading)
    {
        return
            (reading.Metric.Equals("Temperature",
                StringComparison.OrdinalIgnoreCase) && reading.Value > 70)
            ||
            (reading.Metric.Equals("Power",
                StringComparison.OrdinalIgnoreCase) && reading.Value > 2000)
            ||
            (reading.Metric.Equals("Moisture",
                StringComparison.OrdinalIgnoreCase) && reading.Value < 20);
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("Smart-X telemetry worker started.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _signal.WaitAsync(stoppingToken);

                TelemetryReading? reading;

                lock (_queueLock)
                {
                    if (_priorityQueue.Count > 0)
                    {
                        reading = _priorityQueue.Dequeue();
                    }
                    else if (_normalQueue.Count > 0)
                    {
                        reading = _normalQueue.Dequeue();
                    }
                    else
                    {
                        reading = null;
                    }
                }

                if (reading is not null)
                {
                    await ProcessReadingAsync(reading, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private async Task ProcessReadingAsync(
        TelemetryReading reading,
        CancellationToken cancellationToken)
    {
        try
        {
            int incidentsBefore = _anomalyDetector.GetIncidents().Count;

            _buffer.Add(reading);
            _store.Add(reading);
            _rangeService.Add(reading);
            _anomalyDetector.Analyze(reading);
            _sensorStore.UpdateLastTelemetry(reading.DeviceId);

            await _hubContext.Clients.All.SendAsync(
                "ReceiveTelemetry", reading, cancellationToken);

            var incidents = _anomalyDetector.GetIncidents();

            for (int i = incidentsBefore; i < incidents.Count; i++)
            {
                await _hubContext.Clients.All.SendAsync(
                    "ReceiveIncident", incidents[i], cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to process telemetry for device {DeviceId}.",
                reading.DeviceId);
        }
    }

    public override void Dispose()
    {
        _signal.Dispose();
        base.Dispose();
    }
}