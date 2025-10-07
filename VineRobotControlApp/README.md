# Vine Robot Control App

This Visual Studio solution contains a WPF desktop application for commanding the vine robot's ESP32 controller using PSI setpoints instead of raw ADC values. The user interface is designed around two major workflows:

1. **Setpoints tab** – Operators enter the target pressure (in PSI) for each pouch segment. Per-side calibration tables translate PSI inputs to their expected ADC ranges so that the firmware can map setpoints correctly.
2. **Monitoring tab** – Real-time telemetry from the ESP32 is plotted with ScottPlot and listed in a grid after passing through an outlier filter derived from the provided flowchart.

## Project layout

```
VineRobotControlApp
├── VineRobotControlApp.csproj
├── App.xaml / App.xaml.cs
├── MainWindow.xaml / MainWindow.xaml.cs
├── Models
│   ├── PouchSide.cs
│   ├── PressureCalibration.cs
│   ├── SegmentSetpoint.cs
│   └── TelemetryReading.cs
├── Services
│   ├── SerialMessenger.cs
│   └── OutlierFilter.cs
├── ViewModels
│   └── MainViewModel.cs
└── README.md (this file)
```

## Serial protocol

* **Setpoints** – The application sends commands in the format `SET,<side>,<segment>,<psi>` where `<side>` is `L` or `R`, `<segment>` is 1–3, and `<psi>` is a floating-point value formatted with two decimals. Multiple setpoints can be transmitted in a single frame separated by semicolons.
* **Telemetry** – The ESP32 should respond with lines starting with `PRS,<side>,<segment>,<adc>` representing the raw ADC reading. The UI converts ADC readings to PSI using the calibration data and displays both raw and filtered values.

## Working with calibrations

Each side of the robot can have its own calibration window. Operators can change the minimum/maximum PSI and ADC values for the left and right sensors. Every time the calibration is updated the table recalculates the expected ADC for all setpoints.

## Plotting and outlier rejection

ScottPlot (via the `ScottPlot.WPF` package) renders the filtered and raw PSI traces. Incoming telemetry samples are processed by `OutlierFilter`, which mirrors the supplied flowchart: it collects an initial warm-up buffer before continuously averaging in-range samples while rejecting data that deviates from the rolling baseline beyond a configurable PSI delta.

## Building and running

1. Install the .NET 8.0 SDK (or use Visual Studio 2022 17.8+ which includes it).
2. Open the solution in Visual Studio 2022 (or newer).
3. Restore NuGet packages (ScottPlot.WPF and System.IO.Ports).
4. Build and run the `VineRobotControlApp` project.
5. Select the ESP32 serial port, adjust the baud rate if necessary, and press **Connect**.
6. Enter PSI setpoints, send them individually or in bulk, and observe telemetry in the Monitoring tab.

## Next steps

Future iterations can integrate PS4 controller support, servo control, or additional tabs for configuring solenoid valves once the ESP32 firmware is updated to consume the PSI-based commands.
