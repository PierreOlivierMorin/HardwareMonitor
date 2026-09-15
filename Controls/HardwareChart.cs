using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using HardwareMonitor.Models;

namespace HardwareMonitor.Controls;

public class HardwareChart : FrameworkElement
{
    public static readonly DependencyProperty PointsProperty =
        DependencyProperty.Register(nameof(Points), typeof(List<TelemetryPoint>), typeof(HardwareChart),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MetricProperty =
        DependencyProperty.Register(nameof(Metric), typeof(MetricType), typeof(HardwareChart),
            new FrameworkPropertyMetadata(MetricType.Temperature, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty WindowMinutesProperty =
        DependencyProperty.Register(nameof(WindowMinutes), typeof(TimeWindowMinutes), typeof(HardwareChart),
            new FrameworkPropertyMetadata(TimeWindowMinutes.Min5, FrameworkPropertyMetadataOptions.AffectsRender));

    public List<TelemetryPoint>? Points
    {
        get => (List<TelemetryPoint>?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public MetricType Metric
    {
        get => (MetricType)GetValue(MetricProperty);
        set => SetValue(MetricProperty, value);
    }

    public TimeWindowMinutes WindowMinutes
    {
        get => (TimeWindowMinutes)GetValue(WindowMinutesProperty);
        set => SetValue(WindowMinutesProperty, value);
    }

    private Point? _mousePos;
    private readonly Typeface _font = new("Segoe UI");
    private readonly Pen _gridPen;
    private readonly Pen _axisPen;
    private readonly Pen _cursorPen;
    private readonly Pen _cpuPen;
    private readonly Pen _gpuPen;
    private readonly Brush _cpuBrush;
    private readonly Brush _gpuBrush;
    private readonly Brush _textBrush;
    private readonly Brush _dimTextBrush;

    public HardwareChart()
    {
        _gridPen = new Pen(new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)), 1.0);
        _gridPen.Freeze();

        _axisPen = new Pen(new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)), 1.0);
        _axisPen.Freeze();

        _cursorPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)), 1.0)
        {
            DashStyle = DashStyles.Dash
        };
        _cursorPen.Freeze();

        _cpuBrush = new SolidColorBrush(Color.FromRgb(0, 229, 255)); // Cyan
        _cpuBrush.Freeze();
        _cpuPen = new Pen(_cpuBrush, 2.0);
        _cpuPen.Freeze();

        _gpuBrush = new SolidColorBrush(Color.FromRgb(0, 230, 118)); // Emerald Green
        _gpuBrush.Freeze();
        _gpuPen = new Pen(_gpuBrush, 2.0);
        _gpuPen.Freeze();

        _textBrush = new SolidColorBrush(Color.FromRgb(220, 225, 235));
        _textBrush.Freeze();

        _dimTextBrush = new SolidColorBrush(Color.FromArgb(140, 200, 210, 225));
        _dimTextBrush.Freeze();

        ClipToBounds = true;

        MouseMove += (s, e) =>
        {
            _mousePos = e.GetPosition(this);
            InvalidateVisual();
        };

        MouseLeave += (s, e) =>
        {
            _mousePos = null;
            InvalidateVisual();
        };
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        double w = ActualWidth;
        double h = ActualHeight;
        if (w < 40 || h < 40) return;

        double padLeft = 45;
        double padRight = 15;
        double padTop = 15;
        double padBottom = 25;

        double plotW = w - padLeft - padRight;
        double plotH = h - padTop - padBottom;
        if (plotW <= 0 || plotH <= 0) return;

        // Draw background chart area
        var bgRect = new Rect(padLeft, padTop, plotW, plotH);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(40, 15, 20, 30)), null, bgRect, 6, 6);

        // Determine Y bounds based on Metric
        (float yMin, float yMax, string unit, float[] gridSteps) = Metric switch
        {
            MetricType.Temperature => (20f, 100f, "°C", new[] { 20f, 40f, 60f, 80f, 100f }),
            MetricType.ClockGhz => (0f, 6.0f, " GHz", new[] { 0f, 1.5f, 3.0f, 4.5f, 6.0f }),
            _ => (0f, 100f, "%", new[] { 0f, 25f, 50f, 75f, 100f })
        };

        // If data points exceed default max, expand max
        if (Points != null && Points.Count > 0)
        {
            float maxObserved = Metric switch
            {
                MetricType.Temperature => Points.Max(p => Math.Max(p.CpuTemp, p.GpuTemp)),
                MetricType.ClockGhz => Points.Max(p => Math.Max(p.CpuClockGhz, p.GpuClockGhz)),
                _ => Points.Max(p => Math.Max(p.CpuLoad, p.GpuLoad))
            };

            if (maxObserved > yMax)
            {
                yMax = MathF.Ceiling(maxObserved * 1.1f);
            }
        }

        // Horizontal Grid Lines & Labels
        foreach (float step in gridSteps)
        {
            if (step < yMin || step > yMax) continue;
            double yNorm = (step - yMin) / (yMax - yMin);
            double yPos = padTop + plotH - (yNorm * plotH);

            dc.DrawLine(_gridPen, new Point(padLeft, yPos), new Point(padLeft + plotW, yPos));

            var text = new FormattedText(
                $"{step:0.#}{unit}",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                _font,
                10,
                _dimTextBrush,
                1.0
            );
            dc.DrawText(text, new Point(padLeft - text.Width - 6, yPos - text.Height / 2));
        }

        // Time X-Axis Grid Lines
        int minutes = (int)WindowMinutes;
        int timeSlices = 4;
        DateTime now = DateTime.Now;

        for (int i = 0; i <= timeSlices; i++)
        {
            double frac = (double)i / timeSlices;
            double xPos = padLeft + frac * plotW;

            dc.DrawLine(_gridPen, new Point(xPos, padTop), new Point(xPos, padTop + plotH));

            double minOffset = (1.0 - frac) * minutes;
            string timeLabel = i == timeSlices ? "Maintenant" : $"-{minOffset:0.#}m";

            var text = new FormattedText(
                timeLabel,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                _font,
                9.5,
                _dimTextBrush,
                1.0
            );
            dc.DrawText(text, new Point(xPos - text.Width / 2, padTop + plotH + 5));
        }

        // Draw Plot border
        dc.DrawRectangle(null, _axisPen, bgRect);

        if (Points == null || Points.Count < 2)
        {
            // Empty state message
            var emptyText = new FormattedText(
                "Acquisition des données en cours...",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                _font,
                12,
                _dimTextBrush,
                1.0
            );
            dc.DrawText(emptyText, new Point(padLeft + (plotW - emptyText.Width) / 2, padTop + (plotH - emptyText.Height) / 2));
            return;
        }

        DateTime startTime = now.AddMinutes(-minutes);
        double totalSeconds = minutes * 60.0;

        Point ValueToPoint(DateTime ts, float val)
        {
            double elapsed = (ts - startTime).TotalSeconds;
            double xNorm = Math.Clamp(elapsed / totalSeconds, 0.0, 1.0);
            double yNorm = Math.Clamp((val - yMin) / (yMax - yMin), 0.0, 1.0);

            double x = padLeft + xNorm * plotW;
            double y = padTop + plotH - (yNorm * plotH);
            return new Point(x, y);
        }

        // Helper to extract values
        Func<TelemetryPoint, float> cpuValFunc = Metric switch
        {
            MetricType.Temperature => p => p.CpuTemp,
            MetricType.ClockGhz => p => p.CpuClockGhz,
            _ => p => p.CpuLoad
        };

        Func<TelemetryPoint, float> gpuValFunc = Metric switch
        {
            MetricType.Temperature => p => p.GpuTemp,
            MetricType.ClockGhz => p => p.GpuClockGhz,
            _ => p => p.GpuLoad
        };

        // Render Series Curve & Area
        DrawSeries(dc, Points, cpuValFunc, ValueToPoint, _cpuPen, Color.FromRgb(0, 229, 255), padLeft, plotW, padTop, plotH);
        DrawSeries(dc, Points, gpuValFunc, ValueToPoint, _gpuPen, Color.FromRgb(0, 230, 118), padLeft, plotW, padTop, plotH);

        // Hover cursor inspection
        if (_mousePos.HasValue && bgRect.Contains(_mousePos.Value))
        {
            double mx = _mousePos.Value.X;
            double normX = (mx - padLeft) / plotW;
            DateTime targetTime = startTime.AddSeconds(normX * totalSeconds);

            // Find closest telemetry point
            TelemetryPoint? closest = Points
                .OrderBy(p => Math.Abs((p.Timestamp - targetTime).TotalSeconds))
                .FirstOrDefault();

            if (closest != null)
            {
                Point cpuPt = ValueToPoint(closest.Timestamp, cpuValFunc(closest));
                Point gpuPt = ValueToPoint(closest.Timestamp, gpuValFunc(closest));

                // Vertical guideline
                dc.DrawLine(_cursorPen, new Point(cpuPt.X, padTop), new Point(cpuPt.X, padTop + plotH));

                // Glowing points on curves
                dc.DrawEllipse(_cpuBrush, null, cpuPt, 4, 4);
                dc.DrawEllipse(_gpuBrush, null, gpuPt, 4, 4);

                // Tooltip badge
                string tooltip = $"CPU: {cpuValFunc(closest):0.#}{unit}\nGPU: {gpuValFunc(closest):0.#}{unit}\n{closest.Timestamp:HH:mm:ss}";
                var ttText = new FormattedText(
                    tooltip,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    _font,
                    11,
                    _textBrush,
                    1.0
                );

                double tooltipW = ttText.Width + 16;
                double tooltipH = ttText.Height + 12;
                double tooltipX = Math.Min(cpuPt.X + 10, padLeft + plotW - tooltipW);
                double tooltipY = Math.Max(padTop + 5, Math.Min(cpuPt.Y - tooltipH / 2, padTop + plotH - tooltipH - 5));

                var ttRect = new Rect(tooltipX, tooltipY, tooltipW, tooltipH);
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(235, 10, 15, 25)), new Pen(_axisPen.Brush, 1.0), ttRect, 5, 5);
                dc.DrawText(ttText, new Point(tooltipX + 8, tooltipY + 6));
            }
        }
    }

    private static void DrawSeries(
        DrawingContext dc,
        List<TelemetryPoint> points,
        Func<TelemetryPoint, float> valSelector,
        Func<DateTime, float, Point> toPoint,
        Pen pen,
        Color color,
        double padLeft,
        double plotW,
        double padTop,
        double plotH)
    {
        if (points.Count == 0) return;

        var linePoints = points.Select(p => toPoint(p.Timestamp, valSelector(p))).ToList();
        if (linePoints.Count < 2) return;

        // Draw area fill
        var areaGeom = new StreamGeometry();
        using (var ctx = areaGeom.Open())
        {
            ctx.BeginFigure(new Point(linePoints[0].X, padTop + plotH), true, true);
            foreach (var pt in linePoints)
            {
                ctx.LineTo(pt, true, false);
            }
            ctx.LineTo(new Point(linePoints[^1].X, padTop + plotH), true, false);
        }
        areaGeom.Freeze();

        var gradient = new LinearGradientBrush(
            Color.FromArgb(40, color.R, color.G, color.B),
            Color.FromArgb(0, color.R, color.G, color.B),
            new Point(0, 0),
            new Point(0, 1)
        );
        gradient.Freeze();

        dc.DrawGeometry(gradient, null, areaGeom);

        // Draw line
        var lineGeom = new StreamGeometry();
        using (var ctx = lineGeom.Open())
        {
            ctx.BeginFigure(linePoints[0], false, false);
            for (int i = 1; i < linePoints.Count; i++)
            {
                ctx.LineTo(linePoints[i], true, false);
            }
        }
        lineGeom.Freeze();

        dc.DrawGeometry(null, pen, lineGeom);
    }
}
