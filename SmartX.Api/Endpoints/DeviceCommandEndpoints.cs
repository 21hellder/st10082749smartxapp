
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SmartX.Api.Models;
using SmartX.Api.Services;

namespace SmartX.Api.Endpoints;

public static class DeviceCommandEndpoints
{
    public static void MapDeviceCommandEndpoints(
        this WebApplication app,
        DeviceCommandService commandService)
    {
        // Issue a manual pump or relay command.
        app.MapPost("/api/commands", (CreateDeviceCommandRequest request) =>
        {
            try
            {
                DeviceCommand command = commandService.CreateCommand(
                    request.DeviceId,
                    request.Component,
                    request.Action,
                    request.IssuedBy ?? "Dashboard");

                return Results.Created(
                    $"/api/commands/{command.Id}",
                    new
                    {
                        success = true,
                        message = "Command recorded successfully.",
                        command
                    });
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = exception.Message
                });
            }
        });

        // View all recorded commands, newest first.
        app.MapGet("/api/commands", () =>
        {
            return Results.Ok(new
            {
                success = true,
                commands = commandService.GetHistory()
            });
        });

        // View the most recent command that has not been undone.
        app.MapGet("/api/commands/last", () =>
        {
            DeviceCommand? command = commandService.GetLastCommand();

            return command is null
                ? Results.NotFound(new
                {
                    success = false,
                    message = "No active command exists in the history."
                })
                : Results.Ok(new
                {
                    success = true,
                    command
                });
        });

        // Mark the latest command as undone.
        app.MapPost("/api/commands/undo", () =>
        {
            DeviceCommand? command = commandService.UndoLastCommand();

            return command is null
                ? Results.NotFound(new
                {
                    success = false,
                    message = "There are no commands available to undo."
                })
                : Results.Ok(new
                {
                    success = true,
                    message = "The latest command has been marked as undone.",
                    undoneCommand = command
                });
        });
    }
}

public sealed record CreateDeviceCommandRequest(
    string DeviceId,
    string Component,
    string Action,
    string? IssuedBy);
