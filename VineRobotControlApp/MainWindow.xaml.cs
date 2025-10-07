using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using ScottPlot;
using VineRobotControlApp.Models;
using VineRobotControlApp.Services;
using VineRobotControlApp.ViewModels;

namespace VineRobotControlApp;

public partial class MainWindow : Window
{
    private readonly SerialMessenger _serial = new();
    private readonly OutlierFilter _filter = new();
    private readonly List<double> _rawPsiPoints = new();
    private readonly List<double> _filteredPsiPoints = new();
    private readonly List<double> _sampleIndex = new();
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainViewModel();
        DataContext = ViewModel;

        Loaded += (_, _) => RefreshPorts();
        Closing += (_, _) => _serial.Dispose();

        _serial.ConnectionStateChanged += OnConnectionStateChanged;
        _serial.MessageReceived += (_, message) => Dispatcher.Invoke(() => AppendLog($"RX: {message}"));
        _serial.MessageSent += (_, message) => Dispatcher.Invoke(() => AppendLog($"TX: {message}"));
        _serial.TelemetryReceived += OnTelemetryReceived;

        ConfigurePlot();
    }

    private void ConfigurePlot()
    {
        var plot = PressurePlot.Plot;
        plot.Title("Pressure History");
        plot.XLabel("Sample");
        plot.YLabel("PSI");
        plot.ShowLegend(Alignment.UpperRight);
        PressurePlot.Refresh();
    }

    private void UpdatePressurePlot()
    {
        var plot = PressurePlot.Plot;
        plot.Clear();

        if (_sampleIndex.Count > 0)
        {
            var xs = _sampleIndex.ToArray();
            var raw = plot.Add.Scatter(xs, _rawPsiPoints.ToArray());
            raw.LegendText = "Raw";

            var filtered = plot.Add.Scatter(xs, _filteredPsiPoints.ToArray());
            filtered.LegendText = "Filtered";
        }

        plot.Axes.AutoScale();
        plot.Title("Pressure History");
        plot.XLabel("Sample");
        plot.YLabel("PSI");
        plot.ShowLegend(Alignment.UpperRight);
        PressurePlot.Refresh();
    }

    private void AppendLog(string message)
    {
        ViewModel.SerialLogs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        while (ViewModel.SerialLogs.Count > 250)
            ViewModel.SerialLogs.RemoveAt(0);
    }

    private void RefreshPorts()
    {
        PortCombo.ItemsSource = SerialMessenger.ListPorts();
        if (PortCombo.Items.Count > 0 && PortCombo.SelectedIndex < 0)
        {
            PortCombo.SelectedIndex = 0;
        }
    }

    private void RefreshPorts_Click(object sender, RoutedEventArgs e) => RefreshPorts();

    private void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_serial.IsOpen)
            {
                _serial.Disconnect();
                return;
            }

            if (PortCombo.SelectedItem is not string port)
            {
                MessageBox.Show("Please select a COM port before connecting.", "Serial", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(BaudText.Text, out int baud))
            {
                MessageBox.Show("Invalid baud rate.", "Serial", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _serial.Connect(port, baud);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Serial", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnConnectionStateChanged(object? sender, bool isConnected)
    {
        Dispatcher.Invoke(() =>
        {
            ConnectButton.Content = isConnected ? "Disconnect" : "Connect";
            ConnectionProgress.Value = isConnected ? 1 : 0;
            ViewModel.StatusMessage = isConnected ? "Connected" : "Disconnected";
            if (!isConnected)
            {
                _filter.Reset();
            }
        });
    }

    private async void SendSelected_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedSetpoint is null)
            return;

        await TrySendSetpointAsync(ViewModel.SelectedSetpoint);
    }

    private void SelectSetpoint_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SegmentSetpoint sp })
        {
            ViewModel.SelectedSetpoint = sp;
        }
    }

    private void SetpointGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid { SelectedItem: SegmentSetpoint sp })
        {
            ViewModel.SelectedSetpoint = sp;
        }
    }

    private async void SendAll_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _serial.SendBulkSetpointsAsync(ViewModel.Setpoints);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Serial", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void SendSetpoint_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SegmentSetpoint sp })
        {
            await TrySendSetpointAsync(sp);
        }
    }

    private async Task TrySendSetpointAsync(SegmentSetpoint setpoint)
    {
        try
        {
            await _serial.SendSetpointAsync(setpoint);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Serial", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SetpointGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.Column.DisplayIndex != 2)
            return;

        if (e.EditingElement is TextBox tb && e.Row.Item is SegmentSetpoint sp)
        {
            if (double.TryParse(tb.Text, out double psi))
            {
                sp.Psi = psi;
            }
        }
    }

    private void OnTelemetryReceived(object? sender, PressureTelemetryEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            var calibration = e.Side == PouchSide.Left ? ViewModel.LeftCalibration : ViewModel.RightCalibration;
            double psiRaw = calibration.AdcToPsi(e.Adc);
            bool isOutlier;
            double psiFiltered = _filter.Process(psiRaw, out isOutlier);

            var reading = new TelemetryReading
            {
                Timestamp = e.Timestamp.ToLocalTime(),
                Side = e.Side,
                Segment = e.Segment,
                Adc = e.Adc,
                PsiRaw = psiRaw,
                PsiFiltered = psiFiltered,
                IsOutlier = isOutlier
            };

            ViewModel.Telemetry.Add(reading);
            while (ViewModel.Telemetry.Count > 500)
                ViewModel.Telemetry.RemoveAt(0);

            double nextIndex = _sampleIndex.Count > 0 ? _sampleIndex[^1] + 1 : 0;
            _sampleIndex.Add(nextIndex);
            _rawPsiPoints.Add(psiRaw);
            _filteredPsiPoints.Add(psiFiltered);

            const int maxPoints = 600;
            if (_sampleIndex.Count > maxPoints)
            {
                _sampleIndex.RemoveAt(0);
                _rawPsiPoints.RemoveAt(0);
                _filteredPsiPoints.RemoveAt(0);
            }

            UpdatePressurePlot();
        });
    }
}
