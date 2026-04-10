namespace SafeHarbor.Services;

/// <summary>
/// Computes a rule-based donor lapse risk score derived from the
/// donor-lapse-explanatory-analysis.ipynb coefficient analysis.
///
/// The logistic regression was explanatory-only (CV AUC &lt; 0.5 on 60 rows),
/// so we deploy the coefficient-derived thresholds as deterministic rules
/// rather than direct model predictions.
/// </summary>
public interface IDonorRiskFlagService
{
    DonorRiskFlag ComputeFlag(DonorRiskInput input);
}

public record DonorRiskInput(
    double DaysSinceLastDonation,
    double DonationFrequency,   // donations per day since first donation
    int    TotalDonations,
    int    UniqueChannels,
    double AvgDaysBetween);

public record DonorRiskFlag(string Level, int Score, string[] Signals);

public sealed class DonorRiskFlagService : IDonorRiskFlagService
{
    public DonorRiskFlag ComputeFlag(DonorRiskInput input)
    {
        int score = 0;
        var signals = new List<string>();

        if (input.DaysSinceLastDonation > 180) { score++; signals.Add("No donation in 6+ months"); }
        if (input.DonationFrequency < 0.01)    { score++; signals.Add("Low donation frequency"); }
        if (input.TotalDonations <= 1)         { score++; signals.Add("Only one donation on record"); }
        if (input.UniqueChannels >= 3)         { score++; signals.Add("Spread across many channels (sporadic giving)"); }
        if (input.AvgDaysBetween > 120)        { score++; signals.Add("Long average gap between gifts"); }

        var level = score >= 4 ? "High" : score >= 2 ? "Medium" : "Low";
        return new DonorRiskFlag(level, score, [.. signals]);
    }
}
