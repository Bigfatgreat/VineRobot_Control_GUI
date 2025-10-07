using System.IO.Ports;
using System.Linq;
using System.Text;
using VineRobotControlApp.Models;

namespace VineRobotControlApp.Services;

public class SerialMessenger : IDisposable
{
    private SerialPort? _serialPort;
    public event EventHandler<string>? MessageTransmitted;

    public bool IsConnected => _serialPort?.IsOpen == true;
    public string? ConnectedPort => _serialPort?.PortName;

    public string[] GetPortNames()
    {
        try
        {
            return SerialPort.GetPortNames().OrderBy(p => p).ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public void Connect(string portName, int baudRate)
    {
        if (string.IsNullOrWhiteSpace(portName))
        {
            throw new ArgumentException("Port name cannot be empty", nameof(portName));
        }

        Disconnect();
        _serialPort = new SerialPort(portName, baudRate)
        {
            Encoding = Encoding.ASCII,
            NewLine = "\n"
        };
        _serialPort.Open();
    }

    public void Disconnect()
    {
        if (_serialPort is { IsOpen: true })
        {
            _serialPort.Close();
        }
        _serialPort?.Dispose();
        _serialPort = null;
    }

    public void SendSetpoint(SegmentSide side, int segmentIndex, double targetPsi)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Serial port is not connected");
        }

        string payload = $"SET,{side},{segmentIndex},{targetPsi:F2}";
        _serialPort!.WriteLine(payload);
        MessageTransmitted?.Invoke(this, payload);
    }

    public void Dispose()
    {
        Disconnect();
    }
}
