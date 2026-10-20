
using SmartX.Api.Models;

namespace SmartX.Api.Services;

public class DeviceCommandService
{
    private readonly Stack<DeviceCommand> _commandHistory = new();
    private readonly List<DeviceCommand> _allCommands = new();
    private readonly object _lock = new();

    public DeviceCommand CreateCommand(
        string? deviceId,
        string? component,
        string? action,
        string? issuedBy = "Dashboard")
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException(
                "Device ID is required.", nameof(deviceId));
        }

        if (string.IsNullOrWhiteSpace(component) ||
            (!component.Equals("Pump", StringComparison.OrdinalIgnoreCase) &&
             !component.Equals("Relay", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException(
                "Component must be Pump or Relay.", nameof(component));
        }

        if (string.IsNullOrWhiteSpace(action) ||
            (!action.Equals("ON", StringComparison.OrdinalIgnoreCase) &&
             !action.Equals("OFF", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException(
                "Action must be ON or OFF.", nameof(action));
        }

        var command = new DeviceCommand
        {
            DeviceId = deviceId.Trim(),
            Component = component.Trim(),
            Action = action.Trim().ToUpperInvariant(),
            IssuedBy = string.IsNullOrWhiteSpace(issuedBy)
                ? "Dashboard"
                : issuedBy.Trim(),
            Timestamp = DateTime.UtcNow
        };

        lock (_lock)
        {
            _commandHistory.Push(command);
            _allCommands.Add(command);
        }

        return command;
    }

    public DeviceCommand? UndoLastCommand()
    {
        lock (_lock)
        {
            while (_commandHistory.Count > 0)
            {
                DeviceCommand command = _commandHistory.Pop();

                if (command.IsUndone)
                    continue;

                command.IsUndone = true;
                return command;
            }

            return null;
        }
    }

    public List<DeviceCommand> GetHistory()
    {
        lock (_lock)
        {
            return _allCommands
                .OrderByDescending(command => command.Timestamp)
                .ToList();
        }
    }

    public DeviceCommand? GetLastCommand()
    {
        lock (_lock)
        {
            return _commandHistory.Count > 0
                ? _commandHistory.Peek()
                : null;
        }
    }
}
