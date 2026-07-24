using System.Text.Json;
using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Import;

/// <summary>
/// Moves one course's history from the exported CosmosDB JSON into the relational model.
///
/// Deliberately bypasses Ahk.Web.Services and writes through the DbContext directly: this is bulk movement of
/// records that are already in final shape, not domain operations. It does reuse <see cref="Normalize"/> so the
/// imported rows are identical in shape to runtime-written ones.
///
/// There is no HTTP scope here, so every read uses IgnoreQueryFilters — the course query filter would otherwise
/// match nothing.
/// </summary>
internal sealed class Importer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly ApplicationDbContext db;
    private readonly ImportOptions options;

    // Caches so each distinct neptun/repo produces exactly one row.
    private readonly Dictionary<string, Student> students = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Submission> submissions = new(StringComparer.Ordinal);

    public Importer(ApplicationDbContext db, ImportOptions options)
    {
        this.db = db;
        this.options = options;
    }

    public async Task<int> RunAsync()
    {
        var course = await db.Courses.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Slug == options.CourseSlug);
        if (course is null)
        {
            Console.Error.WriteLine($"Course '{options.CourseSlug}' does not exist. Create it in the portal first.");
            return 1;
        }

        if (!options.Force && await HasExistingDataAsync(course.Id))
        {
            Console.Error.WriteLine($"Course '{options.CourseSlug}' already has imported data. Re-run with --force to import anyway.");
            return 1;
        }

        var grades = ReadDocuments<StudentResultDocument>(options.GradesFile);
        var events = ReadDocuments<StatusEventDocument>(options.EventsFile);
        var tokens = ReadDocuments<WebhookTokenDocument>(options.TokensFile);

        Console.WriteLine($"Read {grades.Count} grade, {events.Count} event, {tokens.Count} token document(s).");

        await using var transaction = await db.Database.BeginTransactionAsync();

        // Order matters for the foreign keys: students and submissions first, then their children.
        await PreloadExistingAsync(course.Id);

        var eventCount = await ImportEventsAsync(course.Id, events);
        var gradeCount = await ImportGradesAsync(course.Id, grades);
        var tokenCount = await ImportTokensAsync(course.Id, tokens);

        await transaction.CommitAsync();

        Console.WriteLine();
        Console.WriteLine("Import complete:");
        Console.WriteLine($"  students     : {students.Count}");
        Console.WriteLine($"  submissions  : {submissions.Count}");
        Console.WriteLine($"  events       : {eventCount}");
        Console.WriteLine($"  grades       : {gradeCount}");
        Console.WriteLine($"  tokens       : {tokenCount}");
        return 0;
    }

    private Task<bool> HasExistingDataAsync(int courseId)
        => db.Submissions.IgnoreQueryFilters().AnyAsync(s => s.CourseId == courseId);

    private async Task PreloadExistingAsync(int courseId)
    {
        foreach (var s in await db.Students.IgnoreQueryFilters().Where(s => s.CourseId == courseId).ToListAsync())
            students[s.Neptun] = s;

        foreach (var s in await db.Submissions.IgnoreQueryFilters().Where(s => s.CourseId == courseId).ToListAsync())
            submissions[s.GitHubRepoName] = s;
    }

    private static List<T> ReadDocuments<T>(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return new List<T>();

        if (!File.Exists(path))
            throw new FileNotFoundException($"Export file not found: {path}", path);

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<List<T>>(stream, JsonOptions) ?? new List<T>();
    }

    private bool IsIncluded(string? repository)
    {
        if (string.IsNullOrWhiteSpace(repository))
            return false;

        return options.RepoPrefix is null
            || Normalize.RepoName(repository).StartsWith(Normalize.RepoName(options.RepoPrefix), StringComparison.Ordinal);
    }

    private async Task<Student> GetStudentAsync(int courseId, string neptun)
    {
        var key = Normalize.Neptun(neptun);
        if (students.TryGetValue(key, out var existing))
            return existing;

        var student = new Student { CourseId = courseId, Neptun = key };
        db.Students.Add(student);
        await db.SaveChangesAsync();

        students[key] = student;
        return student;
    }

    private async Task<Submission> GetSubmissionAsync(int courseId, string repository, string? neptun)
    {
        var key = Normalize.RepoName(repository);
        if (!submissions.TryGetValue(key, out var submission))
        {
            submission = new Submission { CourseId = courseId, GitHubRepoName = key };
            db.Submissions.Add(submission);
            await db.SaveChangesAsync();
            submissions[key] = submission;
        }

        if (!string.IsNullOrWhiteSpace(neptun) && submission.StudentId is null)
        {
            var student = await GetStudentAsync(courseId, neptun);
            submission.StudentId = student.Id;
            await db.SaveChangesAsync();
        }

        return submission;
    }

    private async Task<int> ImportEventsAsync(int courseId, List<StatusEventDocument> documents)
    {
        var imported = 0;

        foreach (var doc in documents.Where(d => IsIncluded(d.Repository)))
        {
            var submission = await GetSubmissionAsync(courseId, doc.Repository!, doc.Neptun);

            SubmissionEvent? entity = doc.Type switch
            {
                LegacyEventTypes.RepositoryCreate => new RepositoryCreatedEvent(),
                LegacyEventTypes.BranchCreate => new BranchCreatedEvent { Branch = doc.Branch ?? string.Empty },
                LegacyEventTypes.WorkflowRun => new WorkflowRunEvent { Conclusion = doc.Conclusion },
                LegacyEventTypes.PullRequest => new PullRequestEvent
                {
                    Number = doc.Number,
                    Action = doc.Action ?? string.Empty,
                    HtmlUrl = doc.HtmlUrl,
                    Neptun = string.IsNullOrWhiteSpace(doc.Neptun) ? null : Normalize.Neptun(doc.Neptun),
                    Assignees = doc.Assignees ?? new List<string>(),
                },
                _ => null,
            };

            if (entity is null)
            {
                Console.Error.WriteLine($"  ! skipping event with unknown $type '{doc.Type}' (id {doc.Id})");
                continue;
            }

            entity.CourseId = courseId;
            entity.SubmissionId = submission.Id;
            entity.Timestamp = doc.Timestamp;

            db.SubmissionEvents.Add(entity);

            if (submission.LastEventAt is null || doc.Timestamp > submission.LastEventAt)
                submission.LastEventAt = doc.Timestamp;

            imported++;

            if (imported % 500 == 0)
                await db.SaveChangesAsync();
        }

        await db.SaveChangesAsync();
        return imported;
    }

    private async Task<int> ImportGradesAsync(int courseId, List<StudentResultDocument> documents)
    {
        var imported = 0;

        foreach (var doc in documents.Where(d => IsIncluded(d.GitHubRepoName)))
        {
            var neptun = Normalize.Neptun(doc.Neptun ?? string.Empty);
            var submission = await GetSubmissionAsync(courseId, doc.GitHubRepoName!, neptun);

            var record = new GradeRecord
            {
                CourseId = courseId,
                SubmissionId = submission.Id,
                StudentId = submission.StudentId,
                Neptun = neptun,
                PrNumber = doc.GitHubPrNumber,
                PrUrl = doc.GitHubPrUrl,
                Date = doc.Date,
                Actor = doc.Actor,
                Origin = doc.Origin,
                Confirmed = doc.Confirmed,
            };

            var order = 0;
            foreach (var p in doc.Points ?? new List<ExerciseWithPointDocument>())
                record.Points.Add(new GradeExercisePoint { Name = p.Name ?? string.Empty, Point = p.Point, Order = order++ });

            db.GradeRecords.Add(record);
            imported++;

            if (imported % 500 == 0)
                await db.SaveChangesAsync();
        }

        await db.SaveChangesAsync();
        return imported;
    }

    private async Task<int> ImportTokensAsync(int courseId, List<WebhookTokenDocument> documents)
    {
        var imported = 0;

        foreach (var doc in documents.Where(d => !string.IsNullOrWhiteSpace(d.Id)))
        {
            var exists = await db.CourseWebhookTokens.IgnoreQueryFilters().AnyAsync(t => t.Token == doc.Id);
            if (exists)
                continue;

            db.CourseWebhookTokens.Add(new CourseWebhookToken
            {
                CourseId = courseId,
                Token = doc.Id!,
                Secret = doc.Secret ?? string.Empty,
                Description = doc.Description,
            });
            imported++;
        }

        await db.SaveChangesAsync();
        return imported;
    }
}
