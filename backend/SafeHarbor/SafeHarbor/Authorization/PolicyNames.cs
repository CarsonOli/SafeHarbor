namespace SafeHarbor.Authorization;

public static class PolicyNames
{
    public const string AdminOnly = "AdminOnly";
    public const string StaffOrAdmin = "StaffOrAdmin";
    public const string SocialWorkerOnly = "SocialWorkerOnly";
    public const string AuthenticatedUser = "AuthenticatedUser";

    /// <summary>
    /// Policy for donor-facing endpoints.
    /// Only users with the "Donor" role may access routes guarded by this policy.
    /// TODO: Enforce this with [Authorize(Policy = PolicyNames.DonorOnly)] in DonorDashboardController
    ///       once Microsoft Entra ID authentication is wired. Currently [AllowAnonymous] is used.
    /// </summary>
    public const string DonorOnly = "DonorOnly";
}
