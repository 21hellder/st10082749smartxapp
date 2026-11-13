using SmartX.Api.Endpoints;
using SmartX.Api.Hubs;
using SmartX.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("SmartXClient", policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// SignalR
builder.Services.AddSignalR();

// Existing Part 1 services
builder.Services.AddSingleton<TelemetryValidationService>();
builder.Services.AddSingleton<TelemetryStore>();
builder.Services.AddSingleton<TelemetryBatchBuffer>();
builder.Services.AddSingleton<TelemetrySimulationService>();
builder.Services.AddSingleton<AnomalyDetectionService>();
builder.Services.AddSingleton<SensorStore>();
builder.Services.AddSingleton<SensorAttachmentStore>();
builder.Services.AddSingleton<SensorHierarchyService>();

// Part 2: telemetry processing worker
builder.Services.AddSingleton<TelemetryProcessingService>();
builder.Services.AddHostedService(provider =>
    provider.GetRequiredService<TelemetryProcessingService>());

// Part 2: device commands and undo history
builder.Services.AddSingleton<DeviceCommandService>();

// Part 2: predictive recommendations
builder.Services.AddSingleton<RecommendationEngineService>();

// Part 3: Network Topology and Mesh Routing
builder.Services.AddSingleton<TelemetryRangeService>();
builder.Services.AddSingleton<TopologyStore>();

var app = builder.Build();

app.UseCors("SmartXClient");

// API status
app.MapGet("/api/status", () => new
{
    success = true,
    message = "Smart-X API is running."
});

// Existing Part 1 endpoints
app.MapTelemetryEndpoints();

app.MapSensorEndpoints(
    app.Services.GetRequiredService<SensorStore>());

// Part 2: device commands
app.MapDeviceCommandEndpoints(
    app.Services.GetRequiredService<DeviceCommandService>());

// Part 2: recommendation engine
app.MapRecommendationEndpoints();

// Part 3: topology endpoints
app.MapTopologyEndpoints();

// SignalR telemetry hub
app.MapHub<TelemetryHub>("/hubs/telemetry");

app.Run();