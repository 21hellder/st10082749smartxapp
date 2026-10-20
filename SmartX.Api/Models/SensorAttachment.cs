namespace SmartX.Api.Models;

public class SensorAttachment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string SensorId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public string FilePath { get; set; } = string.Empty;
}