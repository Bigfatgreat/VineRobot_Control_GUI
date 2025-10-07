using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using VineRobotControlApp.Models;

namespace VineRobotControlApp.Services;

public sealed class SerialMessenger : IDisposable
{
    private readonly object _syncRoot = new();

    private SerialPort? _port;
    private CancellationTokenSource? _cts;
    private Channel<string>? _txChannel;
    private Task? _readTask;
    private Task? _writeTask;

    public event EventHandler<bool>? ConnectionStateChanged;
    public event EventHandler<string>? MessageReceived;
    public event EventHandler<string>? MessageSent;
    public event EventHandler<SerialErrorReceivedEventArgs>? ErrorReceived;
    public event EventHandler<PressureTelemetryEventArgs>? TelemetryReceived;

    public bool IsOpen => _port?.IsOpen ?? false;

    public void Connect(string portName, int baudRate = 115200)
    {
        bool connected = false;

        lock (_syncRoot)
        {
            if (IsOpen)
                throw new InvalidOperationException("Serial port is already open.");

            _cts = new CancellationTokenSource();
            _txChannel = Channel.CreateUnbounded<string>();

            var port = new SerialPort
            {
                PortName = portName,
                BaudRate = baudRate,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Encoding = Encoding.ASCII,
                NewLine = "\n"
            };

            try
            {
                port.ErrorReceived += HandleSerialError;
                port.Open();

                _port = port;
                _readTask = Task.Run(() => ReadLoop(_cts.Token), _cts.Token);
                _writeTask = Task.Run(() => WriteLoopAsync(_cts.Token), _cts.Token);
                connected = true;
            }
            catch
            {
                port.ErrorReceived -= HandleSerialError;
                port.Dispose();
                _txChannel?.Writer.TryComplete();
                _txChannel = null;
                _cts?.Dispose();
                _cts = null;
                throw;
            }
        }

        if (connected)
        {
            ConnectionStateChanged?.Invoke(this, true);
        }
    }

    public void Disconnect()
    {
        bool wasOpen;

        lock (_syncRoot)
        {
            wasOpen = IsOpen;
            if (!wasOpen)
                return;

            _cts?.Cancel();
            _txChannel?.Writer.TryComplete();

            var port = _port;
            if (port != null)
            {
                port.ErrorReceived -= HandleSerialError;
                _port = null;
                try
                {
                    port.Close();
                }
                catch (Exception)
                {
                    // Ignore shutdown errors
                }
            }

            try
            {
                Task.WaitAll(new[] { _readTask, _writeTask }
                    .Where(t => t != null)
                    .Cast<Task>()
                    .ToArray(),
                    TimeSpan.FromMilliseconds(500));
            }
            catch (AggregateException)
            {
                // Background tasks throw OperationCanceledException when shutting down.
            }

            port?.Dispose();

            _cts?.Dispose();
            _cts = null;
            _txChannel = null;
            _readTask = null;
            _writeTask = null;
        }

        if (wasOpen)
        {
            ConnectionStateChanged?.Invoke(this, false);
        }
    }

    public Task SendSetpointAsync(SegmentSetpoint setpoint, CancellationToken cancellationToken = default)
    {
        string sideCode = setpoint.Side == PouchSide.Left ? "L" : "R";
        string command = $"SET,{sideCode},{setpoint.Segment},{setpoint.Psi:F2}";
        return QueueMessageAsync(command, cancellationToken);
    }

    public Task SendBulkSetpointsAsync(IEnumerable<SegmentSetpoint> setpoints, CancellationToken cancellationToken = default)
    {
        string payload = string.Join(';', setpoints.Select(s =>
        {
            string sideCode = s.Side == PouchSide.Left ? "L" : "R";
            return $"SET,{sideCode},{s.Segment},{s.Psi:F2}";
        }));

        return QueueMessageAsync(payload, cancellationToken);
    }

    public async Task QueueMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (!IsOpen || _txChannel == null)
            throw new InvalidOperationException("Serial port is not open.");

        await _txChannel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
    }

    public static string[] ListPorts() => SerialPort.GetPortNames().OrderBy(p => p).ToArray();

    private void ReadLoop(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && _port != null)
            {
                string? line = _port.ReadLine();
                if (line == null)
                    continue;

                ProcessIncomingLine(line.Trim());
            }
        }
        catch (OperationCanceledException)
        {
            // expected when shutting down
        }
        catch (Exception ex)
        {
            MessageReceived?.Invoke(this, $"ERROR: {ex.Message}");
        }
    }

    private async Task WriteLoopAsync(CancellationToken token)
    {
        if (_txChannel == null || _port == null)
            return;

        try
        {
            while (await _txChannel.Reader.WaitToReadAsync(token).ConfigureAwait(false))
            {
                while (_txChannel.Reader.TryRead(out string? message))
                {
                    _port.WriteLine(message);
                    MessageSent?.Invoke(this, message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected when shutting down
        }
    }

    private void ProcessIncomingLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        string[] parts = line.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 4 && parts[0].Equals("PRS", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryParseSide(parts[1], out PouchSide side))
            {
                MessageReceived?.Invoke(this, line);
                return;
            }

            if (!int.TryParse(parts[2], out int segment))
            {
                MessageReceived?.Invoke(this, line);
                return;
            }

            if (!int.TryParse(parts[3], out int adc))
            {
                MessageReceived?.Invoke(this, line);
                return;
            }

            TelemetryReceived?.Invoke(this, new PressureTelemetryEventArgs(side, segment, adc, DateTime.UtcNow));
            return;
        }

        MessageReceived?.Invoke(this, line);
    }

    private static bool TryParseSide(string token, out PouchSide side)
    {
        switch (token.ToUpperInvariant())
        {
            case "L":
            case "LEFT":
                side = PouchSide.Left;
                return true;
            case "R":
            case "RIGHT":
                side = PouchSide.Right;
                return true;
            default:
                side = default;
                return false;
        }
    }

    private void HandleSerialError(object? sender, SerialErrorReceivedEventArgs e)
    {
        ErrorReceived?.Invoke(this, e);
    }

    public void Dispose()
    {
        Disconnect();
    }
}

public record PressureTelemetryEventArgs(PouchSide Side, int Segment, int Adc, DateTime Timestamp);
