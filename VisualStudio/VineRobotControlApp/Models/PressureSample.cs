namespace VineRobotControlApp.Models;

public record PressureSample(DateTime Timestamp, double SensorPsi, double FilteredPsi);
