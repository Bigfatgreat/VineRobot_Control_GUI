using CommunityToolkit.Mvvm.ComponentModel;

namespace VineRobotControlApp.Models;

public class SegmentSetpoint : ObservableObject
{
    private double _psi;
    private int _expectedAdc;
    private bool _isSelected;

    public PouchSide Side { get; }
    public int Segment { get; }
    public PressureCalibration Calibration { get; }

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
            if (SetProperty(ref _psi, value))
            {
                UpdateExpectedAdc();
            }
        }
    }

    public int ExpectedAdc
    {
        get => _expectedAdc;
        private set => SetProperty(ref _expectedAdc, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string DisplayName => $"{Side} S{Segment}";

    public void UpdateExpectedAdc()
    {
        ExpectedAdc = Calibration.PsiToAdc(Psi);
    }

}
