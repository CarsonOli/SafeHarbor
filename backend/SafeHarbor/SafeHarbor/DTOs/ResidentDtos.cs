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
    DateTimeOffset UpdatedAtUtc);

public sealed record ResidentPublicResponse(
    Guid Id,
    string FullName,
    int AgeYears);
