namespace TimeReady.Api.Authorization;

/// <summary>Role names used across the application.</summary>
public static class Roles
{
    /// <summary>Full access, including creating and deleting employees.</summary>
    public const string Admin = "Admin";

    /// <summary>Day-to-day HR work: read everything, update preparation status.</summary>
    public const string Operator = "Operator";

    /// <summary>All roles, in the order they are seeded.</summary>
    public static readonly string[] All = [Admin, Operator];
}

/// <summary>
/// Policy names. Endpoints refer to what a caller may do, not to a role, so a
/// third role could be added without touching the controllers.
/// </summary>
public static class Policies
{
    /// <summary>See employees and readiness results.</summary>
    public const string ReadEmployees = "employees:read";

    /// <summary>Change an existing employee record.</summary>
    public const string UpdateEmployees = "employees:update";

    /// <summary>Create or delete employee records.</summary>
    public const string ManageEmployees = "employees:manage";

    /// <summary>Read the audit trail.</summary>
    public const string ReadAuditTrail = "audit:read";
}
