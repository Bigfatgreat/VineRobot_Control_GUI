using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using VineRobotControlApp.Models;
using VineRobotControlApp.Services;

namespace VineRobotControlApp.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly SerialMessenger _serialMessenger;
    private readonly PressureCalibrationService _calibrationService;
    private SegmentSetpointViewModel? _selectedSegment;
    private string? _selectedPort;
    private int _baudRate = 115200;
    private bool _isConnected;

    public MainViewModel()
    {
        _calibrationService = new PressureCalibrationService();
        _serialMessenger = new SerialMessenger();
        _serialMessenger.MessageTransmitted += OnMessageTransmitted;

        Segments = new ObservableCollection<SegmentSetpointViewModel>(CreateSegments());
        LeftSegments = new ObservableCollection<SegmentSetpointViewModel>(Segments.Where(s => s.Side == SegmentSide.Left));
        RightSegments = new ObservableCollection<SegmentSetpointViewModel>(Segments.Where(s => s.Side == SegmentSide.Right));
        SegmentGroups = new ObservableCollection<SegmentGroupViewModel>(
            new[]
            {
                new SegmentGroupViewModel(SegmentSide.Left, LeftSegments),
                new SegmentGroupViewModel(SegmentSide.Right, RightSegments)
            });

        AvailablePorts = new ObservableCollection<string>(_serialMessenger.GetPortNames());
        Telemetry = new ObservableCollection<PressureSample>();
        EventLog = new ObservableCollection<string>();

        RefreshPortsCommand = new RelayCommand(_ => RefreshPorts());
        ConnectCommand = new RelayCommand(_ => Connect(), _ => !IsConnected && !string.IsNullOrWhiteSpace(SelectedPort));
        DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => IsConnected);
        SendSelectedCommand = new RelayCommand(_ => SendSelected(), _ => IsConnected && SelectedSegment is not null);
        SendAllCommand = new RelayCommand(_ => SendAll(), _ => IsConnected);
    }

    public ObservableCollection<SegmentSetpointViewModel> Segments { get; }
    public ObservableCollection<SegmentSetpointViewModel> LeftSegments { get; }
    public ObservableCollection<SegmentSetpointViewModel> RightSegments { get; }
    public ObservableCollection<SegmentGroupViewModel> SegmentGroups { get; }
    public ObservableCollection<PressureSample> Telemetry { get; }
    public ObservableCollection<string> EventLog { get; }
    public ObservableCollection<string> AvailablePorts { get; }

    public ICommand RefreshPortsCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand SendSelectedCommand { get; }
    public ICommand SendAllCommand { get; }

    public SegmentSetpointViewModel? SelectedSegment
    {
        get => _selectedSegment;
        set
        {
            if (SetProperty(ref _selectedSegment, value))
            {
                foreach (var segment in Segments)
                {
                    segment.IsSelected = segment == value;
                }
                ((RelayCommand)SendSelectedCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string? SelectedPort
    {
        get => _selectedPort;
        set
        {
            if (SetProperty(ref _selectedPort, value))
            {
                ((RelayCommand)ConnectCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public int BaudRate
    {
        get => _baudRate;
        set => SetProperty(ref _baudRate, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (SetProperty(ref _isConnected, value))
            {
                ((RelayCommand)ConnectCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DisconnectCommand).RaiseCanExecuteChanged();
                ((RelayCommand)SendSelectedCommand).RaiseCanExecuteChanged();
                ((RelayCommand)SendAllCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public void AddTelemetrySample(double rawPsi, double filteredPsi)
    {
        Telemetry.Add(new PressureSample(DateTime.Now, rawPsi, filteredPsi));
        if (Telemetry.Count > 250)
        {
            Telemetry.RemoveAt(0);
        }
    }

    private IEnumerable<SegmentSetpointViewModel> CreateSegments()
    {
        foreach (SegmentSide side in Enum.GetValues<SegmentSide>())
        {
            for (var segment = 0; segment < 3; segment++)
            {
                yield return new SegmentSetpointViewModel(_calibrationService, side, segment);
            }
        }
    }

    private void RefreshPorts()
    {
        AvailablePorts.Clear();
        foreach (var port in _serialMessenger.GetPortNames())
        {
            AvailablePorts.Add(port);
        }
        Log($"Ports refreshed ({AvailablePorts.Count})");
    }

    private void Connect()
    {
        try
        {
            if (SelectedPort is null)
            {
                return;
            }

            _serialMessenger.Connect(SelectedPort, BaudRate);
            IsConnected = true;
            Log($"Connected to {SelectedPort} @ {BaudRate} baud");
        }
        catch (Exception ex)
        {
            Log($"Connect failed: {ex.Message}");
            Disconnect();
        }
    }

    private void Disconnect()
    {
        if (!IsConnected)
        {
            _serialMessenger.Disconnect();
            return;
        }

        _serialMessenger.Disconnect();
        IsConnected = false;
        Log("Disconnected");
    }

    private void SendSelected()
    {
        if (SelectedSegment is null)
        {
            return;
        }

        SendSegment(SelectedSegment);
    }

    private void SendAll()
    {
        foreach (var segment in Segments)
        {
            SendSegment(segment);
        }
    }

    private void SendSegment(SegmentSetpointViewModel segment)
    {
        try
        {
            _serialMessenger.SendSetpoint(segment.Side, segment.SegmentIndex, segment.DesiredPsi);
            segment.LastSentPsi = segment.DesiredPsi;
            segment.LastSentAt = DateTime.Now;
            Log($"Setpoint sent → {segment.SideLabel} {segment.SegmentLabel}: {segment.DesiredPsi:F2} PSI");
        }
        catch (Exception ex)
        {
            Log($"Send failed ({segment.SideLabel} {segment.SegmentLabel}): {ex.Message}");
        }
    }

    private void OnMessageTransmitted(object? sender, string e)
    {
        Log($"TX: {e}");
    }

    private void Log(string message)
    {
        EventLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        if (EventLog.Count > 200)
        {
            EventLog.RemoveAt(0);
        }
    }
}
