namespace VineRobotControlApp.Models;

public readonly record struct PressureCalibrationResult(double AdcMin, double AdcMax)
{
    public bool IsValid => AdcMax >= AdcMin;
}
