using VineRobotControlApp.Models;
using VineRobotControlApp.Services;

namespace VineRobotControlApp.ViewModels;

public class SegmentSetpointViewModel : ViewModelBase
{
    private readonly PressureCalibrationService _calibrationService;
    private double _desiredPsi;
    private bool _isSelected;
    private DateTime? _lastSentAt;
    private double? _lastSentPsi;

    public SegmentSetpointViewModel(PressureCalibrationService calibrationService, SegmentSide side, int segmentIndex)
    {
        _calibrationService = calibrationService;
        Side = side;
        SegmentIndex = segmentIndex;
        _desiredPsi = 1.0;
    }

    public SegmentSide Side { get; }
    public int SegmentIndex { get; }

    public string SideLabel => Side == SegmentSide.Left ? "Left" : "Right";
    public string SegmentLabel => $"Segment {SegmentIndex + 1}";

    public double DesiredPsi
    {
        get => _desiredPsi;
        set
        {
            if (SetProperty(ref _desiredPsi, Math.Clamp(value, 0, 15)))
            {
                OnPropertyChanged(nameof(Calibration));
            }
        }
    }

    public PressureCalibrationResult Calibration => _calibrationService.CalculateRange(DesiredPsi);

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public DateTime? LastSentAt
    {
        get => _lastSentAt;
        set => SetProperty(ref _lastSentAt, value);
    }

    public double? LastSentPsi
    {
        get => _lastSentPsi;
        set => SetProperty(ref _lastSentPsi, value);
    }
}
