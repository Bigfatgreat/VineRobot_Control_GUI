using System;
using System.Collections.Generic;
using System.Linq;

namespace VineRobotControlApp.Services;

/// <summary>
/// Implements the flowchart-based outlier filter. The filter collects a warmup window
/// ("skip" region) and then continuously averages the in-range samples while rejecting
/// measurements that exceed a configurable delta relative to the rolling baseline.
/// </summary>
public class OutlierFilter
{
    private readonly int _warmupCount;
    private readonly double _limitPsi;
    private readonly Queue<double> _warmup = new();
    private readonly Queue<double> _samples = new();
    private readonly int _maxSamples;

    private double _baseline;
    private bool _isPrimed;

    public OutlierFilter(int warmupCount = 200, int maxSamples = 200, double limitPsi = 0.5)
    {
        _warmupCount = warmupCount;
        _maxSamples = Math.Max(1, maxSamples);
        _limitPsi = Math.Max(0.01, limitPsi);
    }

    public double Process(double psi, out bool isOutlier)
    {
        if (!_isPrimed)
        {
            _warmup.Enqueue(psi);
            if (_warmup.Count >= _warmupCount)
            {
                _baseline = _warmup.Average();
                _isPrimed = true;
                _samples.Clear();
            }

            isOutlier = false;
            return psi;
        }

        if (Math.Abs(psi - _baseline) > _limitPsi)
        {
            // Reject outlier and do not feed it into the moving average
            isOutlier = true;
            return _baseline;
        }

        isOutlier = false;
        _samples.Enqueue(psi);
        while (_samples.Count > _maxSamples)
            _samples.Dequeue();

        _baseline = _samples.Average();
        return _baseline;
    }

    public void Reset()
    {
        _warmup.Clear();
        _samples.Clear();
        _baseline = 0;
        _isPrimed = false;
    }
}
