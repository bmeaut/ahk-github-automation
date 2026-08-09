using System.Diagnostics.CodeAnalysis;

namespace Ahk.Web.Data;

/// <summary>
/// Canonical forms for the two natural keys carried over from the original system. Ported verbatim from
/// <c>grade-management/Ahk.GradeManagement.Data/Normalize.cs</c> so imported rows and runtime-written rows
/// are byte-identical; lookups depend on it.
/// </summary>
public static class Normalize
{
    public static string Neptun(string value) => value is null ? string.Empty : value.ToUpperInvariant().Trim();

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Repo name is normalized to lowercase.")]
    public static string RepoName(string value) => value is null ? string.Empty : value.ToLowerInvariant().Trim();
}
