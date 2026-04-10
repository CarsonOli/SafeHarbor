using System.ComponentModel.DataAnnotations;

namespace SafeHarbor.DTOs;

public sealed record ResidentCreateRequest(
    [property: Required, StringLength(120, MinimumLength = 2)] string FullName,
    [property: Required] DateOnly DateOfBirth,
    [property: Required, EmailAddress] string CaseWorkerEmail,
    [property: StringLength(5_000)] string? MedicalNotes);

public sealed record ResidentUpdateRequest(
    [property: Required, StringLength(120, MinimumLength = 2)] string FullName,
    [property: Required] DateOnly DateOfBirth,
    [property: Required, EmailAddress] string CaseWorkerEmail,
    [property: StringLength(5_000)] string? MedicalNotes);

public sealed record ResidentAdminResponse(
    Guid Id,
    string FullName,
    DateOnly DateOfBirth,
    string CaseWorkerEmail,
    string MedicalNotes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    // ML readiness fields — appended, nullable
    int? TotalVisits = null,
    double? AvgFamilyCooperation = null,
    double? PctPsychCheckupsDone = null,
    int? RiskImprovement = null,
    double? AvgProgressPct = null,
    bool FamilySoloParent = false,
    string? CaseCategory = null,
    double? PctSafetyConcerns = null);

public sealed record ResidentPublicResponse(
    Guid Id,
    string FullName,
    int AgeYears);
