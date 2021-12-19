namespace Ahk.GradeManagement
{
    public static class Normalize
    {
        public static string Neptun(string value) => value.ToUpperInvariant().Trim();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Repo name is normalized to lowercase.")]
        public static string RepoName(string value) => value.ToLowerInvariant().Trim();
    }
}
