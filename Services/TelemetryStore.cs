using System;
using System.Collections.Generic;
using System.Linq;
using HardwareMonitor.Models;

namespace HardwareMonitor.Services;

public class TelemetryStore
{
    private const int MaxCapacity = 3600; // 60 minutes * 60 seconds
    private readonly List<TelemetryPoint> _points = new(MaxCapacity);
    private readonly object _lock = new();

    public TelemetryPoint? Latest { get; private set; }

    public void Add(TelemetryPoint point)
    {
        lock (_lock)
        {
            if (_points.Count >= MaxCapacity)
            {
                _points.RemoveAt(0);
            }
            _points.Add(point);
            Latest = point;
        }
    }

    public List<TelemetryPoint> GetHistory(TimeWindowMinutes window)
    {
        lock (_lock)
        {
            if (_points.Count == 0) return new List<TelemetryPoint>();

            DateTime cutoff = DateTime.Now.AddMinutes(-(int)window);
            return _points.Where(p => p.Timestamp >= cutoff).ToList();
        }
    }

    public MetricStats CalculateStats(Func<TelemetryPoint, float> selector, TimeWindowMinutes window)
    {
        lock (_lock)
        {
            var subset = GetHistory(window);
            if (subset.Count == 0)
            {
                return new MetricStats(0, 0, 0, 0);
            }

            var validValues = subset.Select(selector).Where(v => v > 0).ToList();
            if (validValues.Count == 0)
            {
                float currentVal = Latest != null ? selector(Latest) : 0;
                return new MetricStats(currentVal, currentVal, currentVal, currentVal);
            }

            float current = Latest != null ? selector(Latest) : validValues.Last();
            float min = validValues.Min();
            float max = validValues.Max();
            float avg = validValues.Average();

            return new MetricStats(
                MathF.Round(current, 1),
                MathF.Round(min, 1),
                MathF.Round(max, 1),
                MathF.Round(avg, 1)
            );
        }
    }
}
