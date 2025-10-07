using CommunityToolkit.Mvvm.ComponentModel;

namespace VineRobotControlApp.Models;

public class TelemetryReading : ObservableObject
{
    private double _psiRaw;
    private double _psiFiltered;
    private int _adc;
    private bool _isOutlier;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public PouchSide Side { get; set; }
    public int Segment { get; set; }

    public double PsiRaw
    {
        get => _psiRaw;
        set => SetProperty(ref _psiRaw, value);
    }

    public double PsiFiltered
    {
        get => _psiFiltered;
        set => SetProperty(ref _psiFiltered, value);
    }

    public int Adc
    {
        get => _adc;
        set => SetProperty(ref _adc, value);
    }

    public bool IsOutlier
    {
        get => _isOutlier;
        set => SetProperty(ref _isOutlier, value);
    }
}
