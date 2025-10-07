using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using VineRobotControlApp.Models;

namespace VineRobotControlApp.ViewModels;

public class MainViewModel : ObservableObject
{
    private SegmentSetpoint? _selectedSetpoint;
    private string _statusMessage = "Disconnected";

    public ObservableCollection<SegmentSetpoint> Setpoints { get; }
    public ObservableCollection<TelemetryReading> Telemetry { get; } = new();
    public ObservableCollection<string> SerialLogs { get; } = new();

    public PressureCalibration LeftCalibration { get; }
    public PressureCalibration RightCalibration { get; }

    public MainViewModel()
    {
        LeftCalibration = new PressureCalibration("Left")
        {
            PsiMin = 0,
            PsiMax = 15,
            AdcMin = 520,
            AdcMax = 3400
        };

        RightCalibration = new PressureCalibration("Right")
        {
            PsiMin = 0,
            PsiMax = 15,
            AdcMin = 540,
            AdcMax = 3380
        };

        Setpoints = new ObservableCollection<SegmentSetpoint>(Enumerable.Range(1, 3)
            .SelectMany(seg => new[]
            {
                new SegmentSetpoint(PouchSide.Left, seg, LeftCalibration),
                new SegmentSetpoint(PouchSide.Right, seg, RightCalibration)
            }));

        SelectedSetpoint = Setpoints.FirstOrDefault();
    }

    public SegmentSetpoint? SelectedSetpoint
    {
        get => _selectedSetpoint;
        set
        {
            if (SetProperty(ref _selectedSetpoint, value))
            {
                foreach (var sp in Setpoints)
                    sp.IsSelected = sp == value;
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
}
