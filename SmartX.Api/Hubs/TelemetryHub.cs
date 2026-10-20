using Microsoft.AspNetCore.SignalR;
using SmartX.Api.Models;

namespace SmartX.Api.Hubs;

public class TelemetryHub : Hub
{
    public async Task SendTelemetry(TelemetryReading reading)
    {
        await Clients.All.SendAsync("ReceiveTelemetry", reading);
    }

    public async Task SendIncident(TelemetryIncident incident)
    {
        await Clients.All.SendAsync("ReceiveIncident", incident);
    }
}