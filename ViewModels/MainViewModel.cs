using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using HardwareMonitor.Models;
using HardwareMonitor.Services;

namespace HardwareMonitor.ViewModels;

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly HardwareService _hardwareService;
    private readonly TelemetryStore _telemetryStore;
    private readonly DispatcherTimer _timer;

    private bool _isExpanded = false; // Default: Mini Pill mode
    private bool _isAlwaysOnTop = true;
    private double _windowOpacity = 0.96;
    private TimeWindowMinutes _selectedWindow = TimeWindowMinutes.Min5;
    private MetricType _selectedMetric = MetricType.Temperature;
    private List<TelemetryPoint>? _chartPoints;

    private float _cpuTemp;
    private float _cpuClockGhz;
    private float _cpuLoad;
    private float _gpuTemp;
    private float _gpuHotSpotTemp;
    private float _gpuClockGhz;
    private float _gpuLoad;

    private MetricStats _cpuStats = new(0, 0, 0, 0);
    private MetricStats _gpuStats = new(0, 0, 0, 0);

    public event PropertyChangedEventHandler? PropertyChanged;

    public string CpuName => _hardwareService.CpuName;
    public string GpuName => _hardwareService.GpuName;
    public bool IsElevated => _hardwareService.IsElevated;
    public bool CpuTempAvailable => _hardwareService.CpuTempAvailable;

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetField(ref _isExpanded, value);
    }

    public bool IsAlwaysOnTop
    {
        get => _isAlwaysOnTop;
        set => SetField(ref _isAlwaysOnTop, value);
    }

    public double WindowOpacity
    {
        get => _windowOpacity;
        set => SetField(ref _windowOpacity, value);
    }

    public TimeWindowMinutes SelectedWindow
    {
        get => _selectedWindow;
        set
        {
            if (SetField(ref _selectedWindow, value))
            {
                UpdateHistoryAndStats();
            }
        }
    }

    public MetricType SelectedMetric
    {
        get => _selectedMetric;
        set
        {
            if (SetField(ref _selectedMetric, value))
            {
                UpdateHistoryAndStats();
            }
        }
    }

    public List<TelemetryPoint>? ChartPoints
    {
        get => _chartPoints;
        private set => SetField(ref _chartPoints, value);
    }

    // Live Metrics
    public float CpuTemp { get => _cpuTemp; private set { if (SetField(ref _cpuTemp, value)) OnPropertyChanged(nameof(CpuTempStatusColor)); } }
    public float CpuClockGhz { get => _cpuClockGhz; private set => SetField(ref _cpuClockGhz, value); }
    public float CpuLoad { get => _cpuLoad; private set => SetField(ref _cpuLoad, value); }

    public float GpuTemp { get => _gpuTemp; private set { if (SetField(ref _gpuTemp, value)) OnPropertyChanged(nameof(GpuTempStatusColor)); } }
    public float GpuHotSpotTemp { get => _gpuHotSpotTemp; private set => SetField(ref _gpuHotSpotTemp, value); }
    public float GpuClockGhz { get => _gpuClockGhz; private set => SetField(ref _gpuClockGhz, value); }
    public float GpuLoad { get => _gpuLoad; private set => SetField(ref _gpuLoad, value); }

    // Stats
    public MetricStats CpuStats { get => _cpuStats; private set => SetField(ref _cpuStats, value); }
    public MetricStats GpuStats { get => _gpuStats; private set => SetField(ref _gpuStats, value); }

    public string MetricUnit => SelectedMetric switch
    {
        MetricType.Temperature => "°C",
        MetricType.ClockGhz => "GHz",
        _ => "%"
    };

    // Thermal Status Colors
    public string CpuTempStatusColor => CpuTemp switch
    {
        <= 0 => "#78909C",       // Dim gray if temp unavailable
        < 60 => "#00E676",       // Green (< 60°C)
        < 75 => "#FFD600",       // Yellow (60-75°C)
        < 85 => "#FF9100",       // Orange (75-85°C)
        _ => "#FF1744"           // Red (> 85°C)
    };

    public string GpuTempStatusColor => GpuTemp switch
    {
        <= 0 => "#78909C",
        < 60 => "#00E676",
        < 75 => "#FFD600",
        < 85 => "#FF9100",
        _ => "#FF1744"
    };

    // Commands
    public ICommand ToggleExpandCommand { get; }
    public ICommand TogglePinCommand { get; }
    public ICommand SelectWindowCommand { get; }
    public ICommand SelectMetricCommand { get; }
    public ICommand RestartAsAdminCommand { get; }

    public MainViewModel()
    {
        _hardwareService = new HardwareService();
        _telemetryStore = new TelemetryStore();

        ToggleExpandCommand = new RelayCommand(_ => IsExpanded = !IsExpanded);
        TogglePinCommand = new RelayCommand(_ => IsAlwaysOnTop = !IsAlwaysOnTop);
        SelectWindowCommand = new RelayCommand(p =>
        {
            if (p is string s && int.TryParse(s, out int min))
            {
                SelectedWindow = (TimeWindowMinutes)min;
            }
        });
        SelectMetricCommand = new RelayCommand(p =>
        {
            if (p is string s && Enum.TryParse<MetricType>(s, out var metric))
            {
                SelectedMetric = metric;
            }
        });
        RestartAsAdminCommand = new RelayCommand(_ => HardwareService.RestartAsAdmin());

        // Dispatcher timer for 1-second sample loop
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) => Tick();
        _timer.Start();

        // Initial sample
        Tick();
    }

    private void Tick()
    {
        var point = _hardwareService.Sample();
        _telemetryStore.Add(point);

        CpuTemp = point.CpuTemp;
        CpuClockGhz = point.CpuClockGhz;
        CpuLoad = point.CpuLoad;

        GpuTemp = point.GpuTemp;
        GpuHotSpotTemp = point.GpuHotSpotTemp;
        GpuClockGhz = point.GpuClockGhz;
        GpuLoad = point.GpuLoad;

        UpdateHistoryAndStats();
    }

    private void UpdateHistoryAndStats()
    {
        ChartPoints = _telemetryStore.GetHistory(SelectedWindow);

        Func<TelemetryPoint, float> cpuSelector = SelectedMetric switch
        {
            MetricType.Temperature => p => p.CpuTemp,
            MetricType.ClockGhz => p => p.CpuClockGhz,
            _ => p => p.CpuLoad
        };

        Func<TelemetryPoint, float> gpuSelector = SelectedMetric switch
        {
            MetricType.Temperature => p => p.GpuTemp,
            MetricType.ClockGhz => p => p.GpuClockGhz,
            _ => p => p.GpuLoad
        };

        CpuStats = _telemetryStore.CalculateStats(cpuSelector, SelectedWindow);
        GpuStats = _telemetryStore.CalculateStats(gpuSelector, SelectedWindow);

        OnPropertyChanged(nameof(MetricUnit));
        OnPropertyChanged(nameof(CpuTempAvailable));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public void Dispose()
    {
        _timer.Stop();
        _hardwareService.Dispose();
    }
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
    public void Execute(object? parameter) => _execute(parameter);
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
