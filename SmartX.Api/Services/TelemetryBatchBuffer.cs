using SmartX.Api.Models;

namespace SmartX.Api.Services;

public class TelemetryBatchBuffer
{
    private readonly TelemetryReading[][] _batches;
    private int _currentBatch;
    private int _currentPosition;

    public TelemetryBatchBuffer()
    {
        _batches = new TelemetryReading[10][];

        for (int i = 0; i < _batches.Length; i++)
        {
            _batches[i] = new TelemetryReading[10];
        }
    }

    public void Add(TelemetryReading reading)
    {
        if (_currentBatch >= _batches.Length)
        {
            return;
        }

        _batches[_currentBatch][_currentPosition] = reading;

        _currentPosition++;

        if (_currentPosition >= _batches[_currentBatch].Length)
        {
            _currentPosition = 0;
            _currentBatch++;
        }
    }

    public List<TelemetryReading> TransferToList()
    {
        var readings = new List<TelemetryReading>();

        foreach (var batch in _batches)
        {
            foreach (var reading in batch)
            {
                if (reading != null)
                {
                    readings.Add(reading);
                }
            }
        }

        return readings;
    }
}
