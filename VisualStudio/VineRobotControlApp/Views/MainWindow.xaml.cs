using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using VineRobotControlApp.ViewModels;

namespace VineRobotControlApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += (_, _) => AttachTelemetryListener();
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AttachTelemetryListener();
        RefreshPlot();
    }

    private void AttachTelemetryListener()
    {
        if (ViewModel.Telemetry is INotifyCollectionChanged notify)
        {
            notify.CollectionChanged -= OnTelemetryChanged;
            notify.CollectionChanged += OnTelemetryChanged;
        }
    }

    private void OnTelemetryChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshPlot();
    }

    private void RefreshPlot()
    {
        if (PressurePlot is null)
        {
            return;
        }

        var samples = ViewModel.Telemetry.ToList();
        var plot = PressurePlot.Plot;
        plot.Clear();

        if (samples.Count > 0)
        {
            double[] xs = samples.Select(s => s.Timestamp.ToOADate()).ToArray();
            double[] raw = samples.Select(s => s.SensorPsi).ToArray();
            double[] filtered = samples.Select(s => s.FilteredPsi).ToArray();

            plot.AddScatter(xs, raw, label: "Raw PSI");
            plot.AddScatter(xs, filtered, label: "Filtered PSI");
            plot.XAxis.DateTimeFormat(true);
            plot.XAxis.Label("Time");
            plot.YAxis.Label("PSI");
            plot.Legend(true);
        }

        PressurePlot.Refresh();
    }
}
