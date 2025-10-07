using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VineRobotControlApp.Models;

public class SegmentSetpoint : INotifyPropertyChanged
{
    private double _psi;
    private int _expectedAdc;
    private bool _isSelected;

    public PouchSide Side { get; }
    public int Segment { get; }
    public PressureCalibration Calibration { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public SegmentSetpoint(PouchSide side, int segment, PressureCalibration calibration)
    {
        Side = side;
        Segment = segment;
        Calibration = calibration;
        Calibration.CalibrationChanged += (_, _) => UpdateExpectedAdc();
        UpdateExpectedAdc();
    }

    public double Psi
    {
        get => _psi;
        set
        {
            if (SetField(ref _psi, value))
            {
                UpdateExpectedAdc();
            }
        }
    }

    public int ExpectedAdc
    {
        get => _expectedAdc;
        private set => SetField(ref _expectedAdc, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public string DisplayName => $"{Side} S{Segment}";

    public void UpdateExpectedAdc()
    {
        ExpectedAdc = Calibration.PsiToAdc(Psi);
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
