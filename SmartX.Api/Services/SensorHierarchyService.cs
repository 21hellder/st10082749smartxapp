using SmartX.Api.Models;

namespace SmartX.Api.Services;

public class SensorHierarchyService
{
    public int CountNodesRecursively(SensorNode node)
    {
        int count = 1;

        foreach (var child in node.Children)
        {
            count += CountNodesRecursively(child);
        }

        return count;
    }

    public List<string> GetDeviceIdsRecursively(SensorNode node)
    {
        var deviceIds = new List<string>();

        if (!string.IsNullOrWhiteSpace(node.DeviceId))
        {
            deviceIds.Add(node.DeviceId);
        }

        foreach (var child in node.Children)
        {
            deviceIds.AddRange(GetDeviceIdsRecursively(child));
        }

        return deviceIds;
    }

    public bool ValidateHierarchyRecursively(
        SensorNode node,
        out List<string> errors)
    {
        errors = new List<string>();

        ValidateNodeRecursively(
            node,
            "Root",
            errors);

        return errors.Count == 0;
    }

    private void ValidateNodeRecursively(
        SensorNode node,
        string hierarchyPath,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(node.DeviceId))
        {
            errors.Add(
                $"{hierarchyPath}: Device ID is required.");
        }

        if (string.IsNullOrWhiteSpace(node.Location))
        {
            errors.Add(
                $"{hierarchyPath}: Location is required.");
        }

        foreach (var child in node.Children)
        {
            string childPath =
                $"{hierarchyPath} -> {child.Location}";

            ValidateNodeRecursively(
                child,
                childPath,
                errors);
        }
    }
}