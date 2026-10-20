using Microsoft.AspNetCore.SignalR.Client;
using SmartX.Client.Models;

namespace SmartX.Client.Services;

public class TelemetryHubService
{
    private readonly HubConnection _connection;

    public event Action<TelemetryReading>? TelemetryReceived;
    public event Action<TelemetryIncident>? IncidentReceived;

    public bool IsConnected =>
        _connection.State == HubConnectionState.Connected;

    public TelemetryHubService()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5187/hubs/telemetry")
            .WithAutomaticReconnect()
            .Build();

        _connection.On<TelemetryReading>(
            "ReceiveTelemetry",
            reading =>
            {
                TelemetryReceived?.Invoke(reading);
            });

        _connection.On<TelemetryIncident>(
            "ReceiveIncident",
            incident =>
            {
                IncidentReceived?.Invoke(incident);
            });
    }

    public async Task StartAsync()
    {
        if (_connection.State == HubConnectionState.Disconnected)
        {
            await _connection.StartAsync();
        }
    }

    public async Task StopAsync()
    {
        if (_connection.State != HubConnectionState.Disconnected)
        {
            await _connection.StopAsync();
        }
    }
}