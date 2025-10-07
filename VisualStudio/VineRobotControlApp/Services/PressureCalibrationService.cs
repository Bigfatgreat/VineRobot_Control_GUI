using VineRobotControlApp.Models;

namespace VineRobotControlApp.Services;

/// <summary>
/// Provides a linear conversion between user-entered PSI setpoints and the ADC range expected by the ESP32.
/// The defaults can be tuned once empirical data is collected for each pressure sensor.
/// </summary>
public class PressureCalibrationService
{
    /// <summary>
    /// Minimum PSI supported by the pouch actuators.
    /// </summary>
    public double MinimumPsi { get; set; } = 0.0;

    /// <summary>
    /// Maximum PSI that maps to <see cref="MaximumAdc"/>.
    /// </summary>
    public double MaximumPsi { get; set; } = 15.0;

    /// <summary>
    /// ADC value observed when the system is fully vented.
    /// </summary>
    public double MinimumAdc { get; set; } = 400;

    /// <summary>
    /// ADC value observed at <see cref="MaximumPsi"/>.
    /// </summary>
    public double MaximumAdc { get; set; } = 3600;

    public PressureCalibrationResult CalculateRange(double targetPsi, double tolerancePsi = 0.25)
    {
        targetPsi = Math.Clamp(targetPsi, MinimumPsi, MaximumPsi);
        tolerancePsi = Math.Abs(tolerancePsi);
        var minPsi = Math.Max(MinimumPsi, targetPsi - tolerancePsi);
        var maxPsi = Math.Min(MaximumPsi, targetPsi + tolerancePsi);
        double slope = (MaximumAdc - MinimumAdc) / (MaximumPsi - MinimumPsi);
        double adcMin = MinimumAdc + (minPsi - MinimumPsi) * slope;
        double adcMax = MinimumAdc + (maxPsi - MinimumPsi) * slope;
        return new PressureCalibrationResult(Math.Round(adcMin, 0), Math.Round(adcMax, 0));
    }
}
