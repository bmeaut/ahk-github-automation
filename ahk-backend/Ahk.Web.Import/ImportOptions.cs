namespace Ahk.Web.Import;

/// <summary>Command-line options for the one-time import.</summary>
internal sealed class ImportOptions
{
    public string CourseSlug { get; private set; } = string.Empty;

    public string ConnectionString { get; private set; } = string.Empty;

    public string? GradesFile { get; private set; }

    public string? EventsFile { get; private set; }

    public string? TokensFile { get; private set; }

    /// <summary>Only import rows whose repository name starts with this prefix (when one export covers several courses).</summary>
    public string? RepoPrefix { get; private set; }

    public bool Force { get; private set; }

    public static string Usage =>
        """
        Ahk.Web.Import — one-time CosmosDB -> MSSQL import (throwaway tool).

          --course <slug>          Target course slug (must already exist).                [required]
          --connection <conn>      Target MSSQL connection string.                         [required]
          --grades <file.json>     Exported 'grades' container.
          --events <file.json>     Exported 'events' container.
          --tokens <file.json>     Exported 'webhooktokens' container.
          --repo-prefix <prefix>   Only import repositories starting with this prefix.
          --force                  Import even if the course already has domain rows.

        At least one of --grades / --events / --tokens must be supplied.
        """;

    public static bool TryParse(string[] args, out ImportOptions options, out string? error)
    {
        options = new ImportOptions();
        error = null;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string? Next() => i + 1 < args.Length ? args[++i] : null;

            switch (arg)
            {
                case "--course": options.CourseSlug = Next() ?? string.Empty; break;
                case "--connection": options.ConnectionString = Next() ?? string.Empty; break;
                case "--grades": options.GradesFile = Next(); break;
                case "--events": options.EventsFile = Next(); break;
                case "--tokens": options.TokensFile = Next(); break;
                case "--repo-prefix": options.RepoPrefix = Next(); break;
                case "--force": options.Force = true; break;
                default:
                    error = $"Unknown argument: {arg}";
                    return false;
            }
        }

        if (string.IsNullOrWhiteSpace(options.CourseSlug))
            error = "--course is required.";
        else if (string.IsNullOrWhiteSpace(options.ConnectionString))
            error = "--connection is required.";
        else if (options.GradesFile is null && options.EventsFile is null && options.TokensFile is null)
            error = "At least one of --grades / --events / --tokens must be supplied.";

        return error is null;
    }
}
