using TsaToolbox.Models.Setups;

namespace TsaToolbox.ViewModels;

public class FftViewModel
{
    private readonly FftSetup parameters;

    public FftViewModel(FftSetup parameters)
    {
        this.parameters = parameters;

        // Default values.
        Enabled = false;
        Dt = double.NaN;
        OmegaFrom = 0.5;
        OmegaTo = 10;
        UseRadians = false;
        LogScale = false;
    }

    [Notify]
    public bool Enabled
    {
        get => parameters.Enabled;
        set => parameters.Enabled = value;
    }

    [Notify]
    public double Dt
    {
        get => parameters.Dt;
        set => parameters.Dt = value;
    }

    [Notify]
    public double OmegaFrom
    {
        get => parameters.OmegaFrom;
        set => parameters.OmegaFrom = value;
    }

    [Notify]
    public double OmegaTo
    {
        get => parameters.OmegaTo;
        set => parameters.OmegaTo = value;
    }

    [Notify]
    public bool UseRadians
    {
        get => parameters.UseRadians;
        set => parameters.UseRadians = value;
    }

    [Notify]
    public bool LogScale
    {
        get => parameters.LogScale;
        set => parameters.LogScale = value;
    }
}
