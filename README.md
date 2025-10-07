# Vine Robot Control GUI (Visual Studio)

This repository now contains a WPF prototype that replaces the previous PyQt interface. The new application is designed to communicate PSI setpoints to the ESP32 controller, show the series pouch motor selection, and prepare for future telemetry visualization with ScottPlot.

## Getting started

1. Open `VisualStudio/VineRobotControlApp.sln` in Visual Studio 2022 (or newer).
2. Restore NuGet packages (the project references `ScottPlot.WPF`).
3. Build and run the `VineRobotControlApp` project.

## Features implemented in this iteration

- Serial connection workflow (port selection, refresh, connect, disconnect) targeting 115200 baud by default.
- PSI setpoint editor for both sides and all three pouch segments. Each entry immediately shows the expected ADC range using a configurable linear calibration.
- Highlighted “end view” of the series pouch motors so operators can visually confirm which segment is selected.
- Command buttons to send the selected segment or broadcast all setpoints. Messages follow the format `SET,<Side>,<SegmentIndex>,<PSI>`.
- Event log and telemetry placeholders, including a ScottPlot plot area ready for streaming sensor data after applying the flowchart’s filtering logic.

## Next steps

- Hook the monitoring tab to real sensor data from the ESP32 via the serial connection.
- Implement the outlier filtering pipeline before pushing samples to the plot.
- Extend the calibration service with sensor-specific curves once empirical PSI-to-ADC data is captured.
- Add Bluepad / PS4 controller integration to complement manual setpoint entry when required.
