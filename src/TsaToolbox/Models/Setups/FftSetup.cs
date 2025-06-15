namespace TsaToolbox.Models.Setups;

public class FftSetup
{
    public bool Enabled { get; set; }

    public bool LogScale { get; set; }

    public double Dt { get; set; }

    public double OmegaFrom { get; set; }

    public double OmegaTo { get; set; }

    public bool UseRadians { get; set; }
}
