using CommunityToolkit.Mvvm.ComponentModel;

namespace VineRobotControlApp.Models;

public class PressureCalibration : ObservableObject
{
    private double _psiMin;
    private double _psiMax = 15;
    private int _adcMin = 500;
    private int _adcMax = 3500;

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
            if (SetProperty(ref _psiMin, value))
                OnCalibrationChanged();
        }
    }

    public double PsiMax
    {
        get => _psiMax;
        set
        {
            if (SetProperty(ref _psiMax, value))
                OnCalibrationChanged();
        }
    }

    public int AdcMin
    {
        get => _adcMin;
        set
        {
            if (SetProperty(ref _adcMin, value))
                OnCalibrationChanged();
        }
    }

    public int AdcMax
    {
        get => _adcMax;
        set
        {
            if (SetProperty(ref _adcMax, value))
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

}
