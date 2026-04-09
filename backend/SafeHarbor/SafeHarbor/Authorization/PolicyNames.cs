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
    /// This policy is applied to DonorDashboardController so identity scoping logic runs only
    /// after role-based access has already constrained callers to donor principals.
    /// </summary>
    public const string DonorOnly = "DonorOnly";
}
