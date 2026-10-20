using SmartX.Api.Models;

namespace SmartX.Api.Services;

public class SensorAttachmentStore
{
    private readonly List<SensorAttachment> _attachments = new();

    public void Add(SensorAttachment attachment)
    {
        _attachments.Add(attachment);
    }

    public List<SensorAttachment> GetAll()
    {
        return _attachments;
    }

    public List<SensorAttachment> GetBySensorId(string sensorId)
    {
        return _attachments
            .Where(attachment => attachment.SensorId == sensorId)
            .ToList();
    }

    public SensorAttachment? GetById(string id)
    {
        return _attachments.FirstOrDefault(
            attachment => attachment.Id == id);
    }
}