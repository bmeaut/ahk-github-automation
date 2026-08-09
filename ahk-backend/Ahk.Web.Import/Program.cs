using Ahk.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Import;

/// <summary>
/// Entry point for the one-time CosmosDB -> MSSQL import. This project is throwaway: delete it once every
/// course's history has been migrated.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            Console.WriteLine(ImportOptions.Usage);
            return 0;
        }

        if (!ImportOptions.TryParse(args, out var options, out var error))
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine();
            Console.Error.WriteLine(ImportOptions.Usage);
            return 1;
        }

        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(options.ConnectionString)
            .Options;

        // No HTTP scope: a null current course means the query filter matches nothing, so the importer reads
        // with IgnoreQueryFilters throughout.
        await using var db = new ApplicationDbContext(dbOptions, new NullCurrentCourseProvider());

        try
        {
            return await new Importer(db, options).RunAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Import failed: {ex.Message}");
            return 1;
        }
    }
}
