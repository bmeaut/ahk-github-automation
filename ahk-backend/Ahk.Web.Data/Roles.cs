namespace Ahk.Web.Data;

/// <summary>Site-level (application-wide) role names. Course-level roles are on the membership record.</summary>
public static class Roles
{
    /// <summary>Super-admin: manages courses and their connected GitHub environments across the whole site.</summary>
    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> All = new[] { Admin };
}
