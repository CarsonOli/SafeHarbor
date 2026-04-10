namespace SafeHarbor.Services;

/// <summary>
/// Computes a rule-based reintegration readiness score derived from the
/// reintegration-readiness.ipynb coefficient analysis.
///
/// The logistic regression was explanatory-only (CV AUC &lt; 0.5 on 60 residents),
/// so we deploy the coefficient-derived thresholds as deterministic rules.
/// </summary>
public interface IResidentReadinessFlagService
{
    ResidentReadinessFlag ComputeFlag(ResidentReadinessInput input);
}

public record ResidentReadinessInput(
    int    TotalVisits,
    double AvgFamilyCooperation,  // 1–4 scale
    double PctPsychDone,          // 0.0–1.0
    int    RiskImprovement,       // initial_risk_num minus current_risk_num (positive = improved)
    double AvgProgressPct,        // 0–100
    bool   FamilySoloParent,
    string CaseCategory,          // "Neglected", "Surrendered", etc.
    double PctSafetyConcerns);    // 0.0–1.0

public record ResidentReadinessFlag(string Level, int Score, string Action, string[] Signals);

public sealed class ResidentReadinessFlagService : IResidentReadinessFlagService
{
    public ResidentReadinessFlag ComputeFlag(ResidentReadinessInput input)
    {
        int score = 0;
        var signals = new List<string>();

        // Positive signals
        if (input.TotalVisits >= 20)           { score += 2; signals.Add("High family visit frequency — strong engagement"); }
        else if (input.TotalVisits >= 10)      { score += 1; signals.Add("Moderate family visit frequency"); }

        if (input.AvgFamilyCooperation >= 3.0) { score += 2; signals.Add("Family is cooperative or highly cooperative"); }
        if (input.PctPsychDone >= 0.7)         { score += 1; signals.Add("Strong psychological checkup completion"); }

        if (input.RiskImprovement >= 2)        { score += 2; signals.Add("Significant risk level improvement since admission"); }
        else if (input.RiskImprovement >= 1)   { score += 1; signals.Add("Some risk level improvement since admission"); }

        // Negative signals
        if (input.FamilySoloParent)            { score -= 2; signals.Add("WARNING: Solo parent household — reduced capacity"); }
        if (input.CaseCategory == "Neglected") { score -= 1; signals.Add("WARNING: Neglect case — monitor home environment carefully"); }
        if (input.PctSafetyConcerns >= 0.3)    { score -= 2; signals.Add("WARNING: High rate of safety concerns in visits"); }

        var level  = score >= 4 ? "High" : score >= 2 ? "Medium" : "Low";
        var action = level switch
        {
            "High"   => "Consider scheduling reintegration assessment",
            "Medium" => "Continue monitoring — reassess in 30 days",
            _        => "Additional support needed before reintegration review"
        };

        return new ResidentReadinessFlag(level, score, action, [.. signals]);
    }
}
