using System;

namespace HardwareMonitor.Models;

public record TelemetryPoint(
    DateTime Timestamp,
    float CpuTemp,
    float CpuClockGhz,
    float CpuLoad,
    float GpuTemp,
    float GpuHotSpotTemp,
    float GpuClockGhz,
    float GpuLoad
);

public enum MetricType
{
    Temperature,
    ClockGhz,
    Load
}

public enum TimeWindowMinutes
{
    Min5 = 5,
    Min10 = 10,
    Min15 = 15,
    Min20 = 20,
    Min60 = 60
}

public record MetricStats(
    float Current,
    float Min,
    float Max,
    float Average
);
