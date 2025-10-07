using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VineRobotControlApp.Models;

public class PressureCalibration : INotifyPropertyChanged
{
    private double _psiMin;
    private double _psiMax = 15;
    private int _adcMin = 500;
    private int _adcMax = 3500;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? CalibrationChanged;

    public string Name { get; }

    public PressureCalibration(string name)
    {
        Name = name;
    }

    public double PsiMin
    {
        get => _psiMin;
        set
        {
            if (SetField(ref _psiMin, value))
                OnCalibrationChanged();
        }
    }

    public double PsiMax
    {
        get => _psiMax;
        set
        {
            if (SetField(ref _psiMax, value))
                OnCalibrationChanged();
        }
    }

    public int AdcMin
    {
        get => _adcMin;
        set
        {
            if (SetField(ref _adcMin, value))
                OnCalibrationChanged();
        }
    }

    public int AdcMax
    {
        get => _adcMax;
        set
        {
            if (SetField(ref _adcMax, value))
                OnCalibrationChanged();
        }
    }

    public int PsiToAdc(double psi)
    {
        if (PsiMax <= PsiMin)
            return AdcMin;

        psi = Math.Clamp(psi, PsiMin, PsiMax);
        double slope = (AdcMax - AdcMin) / (PsiMax - PsiMin);
        double adc = AdcMin + (psi - PsiMin) * slope;
        return (int)Math.Round(adc);
    }

    public double AdcToPsi(int adc)
    {
        if (AdcMax <= AdcMin)
            return PsiMin;

        adc = Math.Clamp(adc, AdcMin, AdcMax);
        double slope = (PsiMax - PsiMin) / (AdcMax - AdcMin);
        double psi = PsiMin + (adc - AdcMin) * slope;
        return psi;
    }

    private void OnCalibrationChanged()
    {
        CalibrationChanged?.Invoke(this, EventArgs.Empty);
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
