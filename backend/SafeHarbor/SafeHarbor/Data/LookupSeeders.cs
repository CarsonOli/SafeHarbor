using SafeHarbor.Models.Enums;

namespace SafeHarbor.Data;

public static class LookupSeeders
{
    public static readonly DateTimeOffset SeededAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly IReadOnlyList<(int Id, string Code, string Name)> CaseCategories =
    [
        (1, "SAFETY", "Safety and Protection"),
        (2, "HEALTH", "Health and Wellness"),
        (3, "LEGAL", "Legal and Documentation")
    ];

    public static readonly IReadOnlyList<(int Id, int CaseCategoryId, string Code, string Name)> CaseSubcategories =
    [
        (1, 1, "DV", "Domestic Violence"),
        (2, 1, "TRAFF", "Human Trafficking"),
        (3, 2, "MH", "Mental Health"),
        (4, 3, "IMM", "Immigration Support")
    ];

    public static readonly IReadOnlyList<(int Id, string Code, string Name)> VisitTypes =
    [
        (1, "INTAKE", "Intake Visit"),
        (2, "FOLLOWUP", "Follow-up Visit"),
        (3, "EXIT", "Exit Visit")
    ];

    public static readonly IReadOnlyList<(int Id, string Code, string Name)> ContributionTypes =
    [
        (1, "ONE_TIME", "One-time Gift"),
        (2, "RECURRING", "Recurring Gift"),
        (3, "IN_KIND", "In-kind Support")
    ];

    public static readonly IReadOnlyList<(int Id, StatusDomain Domain, string Code, string Name)> StatusStates =
    [
        (1, StatusDomain.ResidentCase, "OPEN", "Open"),
        (2, StatusDomain.ResidentCase, "ACTIVE", "Active"),
        (3, StatusDomain.ResidentCase, "CLOSED", "Closed"),
        (4, StatusDomain.ResidentAssessment, "DRAFT", "Draft"),
        (5, StatusDomain.ResidentAssessment, "FINAL", "Finalized"),
        (6, StatusDomain.HomeVisit, "SCHEDULED", "Scheduled"),
        (7, StatusDomain.HomeVisit, "COMPLETED", "Completed"),
        (8, StatusDomain.CaseConference, "PLANNED", "Planned"),
        (9, StatusDomain.CaseConference, "HELD", "Held"),
        (10, StatusDomain.InterventionPlan, "PROPOSED", "Proposed"),
        (11, StatusDomain.InterventionPlan, "APPROVED", "Approved"),
        (12, StatusDomain.Campaign, "DRAFT", "Draft"),
        (13, StatusDomain.Campaign, "LIVE", "Live"),
        (14, StatusDomain.Campaign, "ENDED", "Ended"),
        (15, StatusDomain.Contribution, "PLEDGED", "Pledged"),
        (16, StatusDomain.Contribution, "RECEIVED", "Received")
    ];
}
