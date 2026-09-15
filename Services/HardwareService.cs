using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Security.Principal;
using HardwareMonitor.Models;
using LibreHardwareMonitor.Hardware;

namespace HardwareMonitor.Services;

public class HardwareService : IDisposable
{
    private readonly Computer _computer;
    private IHardware? _cpuHardware;
    private IHardware? _gpuHardware;
    private IHardware? _amdApuHardware;
    private PerformanceCounter? _cpuPerfCounter;
    private float _cpuBaseClockGhz = 4.4f; // Default baseline for Ryzen 9 7900X3D (4.4 GHz)

    public bool IsElevated { get; }
    public string CpuName { get; private set; } = "CPU";
    public string GpuName { get; private set; } = "GPU";
    public bool CpuTempAvailable { get; private set; } = false;

    public HardwareService()
    {
        IsElevated = CheckIsElevated();

        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true
        };

        try
        {
            _computer.Open();
            InitHardware();
            InitCpuFrequencyFallback();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing HardwareMonitor: {ex.Message}");
        }
    }

    private static bool CheckIsElevated()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private void InitHardware()
    {
        // CPU
        _cpuHardware = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        if (_cpuHardware != null)
        {
            CpuName = _cpuHardware.Name;
        }

        // Integrated AMD GPU (on the Ryzen processor die)
        _amdApuHardware = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuAmd);

        // Dedicated GPU (Nvidia GeForce RTX 4080 SUPER preferred)
        _gpuHardware = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia)
                       ?? _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuIntel)
                       ?? _amdApuHardware;

        if (_gpuHardware != null)
        {
            GpuName = _gpuHardware.Name;
        }
    }

    private void InitCpuFrequencyFallback()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT MaxClockSpeed, Name FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["MaxClockSpeed"] is uint maxMhz && maxMhz > 0)
                {
                    _cpuBaseClockGhz = maxMhz / 1000f;
                }
            }

            _cpuPerfCounter = new PerformanceCounter("Processor Information", "% Processor Performance", "_Total");
            _cpuPerfCounter.NextValue(); // prime counter
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to init PerformanceCounter: {ex.Message}");
        }
    }

    public TelemetryPoint Sample()
    {
        DateTime now = DateTime.Now;

        float cpuTemp = 0;
        float cpuClockGhz = 0;
        float cpuLoad = 0;

        float gpuTemp = 0;
        float gpuHotSpot = 0;
        float gpuClockGhz = 0;
        float gpuLoad = 0;

        // --- CPU Update ---
        if (_cpuHardware != null)
        {
            _cpuHardware.Update();

            // CPU Temp
            var tempSensor = _cpuHardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature &&
                (s.Name.Contains("Tctl", StringComparison.OrdinalIgnoreCase) ||
                 s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase) ||
                 s.Name.Contains("Core Max", StringComparison.OrdinalIgnoreCase) ||
                 s.Name.Contains("Core (Average)", StringComparison.OrdinalIgnoreCase)));

            if (tempSensor?.Value != null && tempSensor.Value.Value > 0)
            {
                cpuTemp = tempSensor.Value.Value;
                CpuTempAvailable = true;
            }
            else
            {
                // Try any non-zero CPU temperature sensor
                var fallbackTemp = _cpuHardware.Sensors
                    .Where(s => s.SensorType == SensorType.Temperature && s.Value.HasValue && s.Value.Value > 0)
                    .Select(s => s.Value!.Value)
                    .DefaultIfEmpty(0)
                    .Max();

                if (fallbackTemp > 0)
                {
                    cpuTemp = fallbackTemp;
                    CpuTempAvailable = true;
                }
                else
                {
                    CpuTempAvailable = false;
                }
            }

            // Fallback: If CPU temp is 0, query AMD integrated GPU (on the Ryzen processor die / SoC)
            if (cpuTemp <= 0 && _amdApuHardware != null)
            {
                _amdApuHardware.Update();
                var apuTemp = _amdApuHardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature &&
                    (s.Name.Contains("VR SoC", StringComparison.OrdinalIgnoreCase) ||
                     s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase)));

                if (apuTemp?.Value != null && apuTemp.Value.Value > 0)
                {
                    cpuTemp = apuTemp.Value.Value;
                    CpuTempAvailable = true;
                }
            }

            // CPU Load
            var loadSensor = _cpuHardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Total", StringComparison.OrdinalIgnoreCase));
            if (loadSensor?.Value != null)
            {
                cpuLoad = loadSensor.Value.Value;
            }

            // CPU Clock
            var clockSensor = _cpuHardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Clock && 
                (s.Name.Contains("Average", StringComparison.OrdinalIgnoreCase) || s.Name.Contains("Core #1", StringComparison.OrdinalIgnoreCase)));
            
            if (clockSensor?.Value != null && clockSensor.Value.Value > 0)
            {
                cpuClockGhz = clockSensor.Value.Value / 1000f;
            }
        }

        // Fallback for CPU Clock if LHM returned 0 (non-admin)
        if (cpuClockGhz <= 0 && _cpuPerfCounter != null)
        {
            try
            {
                float perfPct = _cpuPerfCounter.NextValue();
                if (perfPct > 0)
                {
                    cpuClockGhz = (_cpuBaseClockGhz * perfPct) / 100f;
                }
            }
            catch
            {
                // Keep 0 or base
            }
        }

        // --- GPU Update ---
        if (_gpuHardware != null)
        {
            _gpuHardware.Update();

            // Core Temp
            var gpuCoreTempSensor = _gpuHardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase));
            if (gpuCoreTempSensor?.Value != null)
            {
                gpuTemp = gpuCoreTempSensor.Value.Value;
            }

            // Hot Spot Temp
            var gpuHotSpotSensor = _gpuHardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name.Contains("Hot", StringComparison.OrdinalIgnoreCase));
            if (gpuHotSpotSensor?.Value != null)
            {
                gpuHotSpot = gpuHotSpotSensor.Value.Value;
            }

            // GPU Clock
            var gpuClockSensor = _gpuHardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Clock && s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase));
            if (gpuClockSensor?.Value != null)
            {
                gpuClockGhz = gpuClockSensor.Value.Value / 1000f;
            }

            // GPU Load
            var gpuLoadSensor = _gpuHardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase));
            if (gpuLoadSensor?.Value != null)
            {
                gpuLoad = gpuLoadSensor.Value.Value;
            }
        }

        return new TelemetryPoint(
            now,
            MathF.Round(cpuTemp, 1),
            MathF.Round(cpuClockGhz, 2),
            MathF.Round(cpuLoad, 1),
            MathF.Round(gpuTemp, 1),
            MathF.Round(gpuHotSpot, 1),
            MathF.Round(gpuClockGhz, 2),
            MathF.Round(gpuLoad, 1)
        );
    }

    public static void RestartAsAdmin()
    {
        try
        {
            var proc = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Process.GetCurrentProcess().MainModule?.FileName ?? Environment.ProcessPath ?? "",
                Verb = "runas"
            };

            Process.Start(proc);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to restart as admin: {ex.Message}");
        }
    }

    public void Dispose()
    {
        try
        {
            _cpuPerfCounter?.Dispose();
            _computer.Close();
        }
        catch
        {
            // Ignore on shutdown
        }
    }
}
