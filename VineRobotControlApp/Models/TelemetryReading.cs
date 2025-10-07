using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VineRobotControlApp.Models;

public class TelemetryReading : INotifyPropertyChanged
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
        set => SetField(ref _psiRaw, value);
    }

    public double PsiFiltered
    {
        get => _psiFiltered;
        set => SetField(ref _psiFiltered, value);
    }

    public int Adc
    {
        get => _adc;
        set => SetField(ref _adc, value);
    }

    public bool IsOutlier
    {
        get => _isOutlier;
        set => SetField(ref _isOutlier, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
