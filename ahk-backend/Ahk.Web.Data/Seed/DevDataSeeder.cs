using Ahk.Web.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ahk.Web.Data.Seed;

/// <summary>
/// Seeds development data: applies migrations, ensures roles, a super-admin, an instructor and a course admin
/// scoped to one course, two sample courses with GitHub config + CI token, and a small amount of realistic data
/// (students, submissions, status events, grades) so the dashboards and exports have something to show.
///
/// All reads use <c>IgnoreQueryFilters()</c>: there is no HTTP request here, so no current course is set and
/// the course query filter would otherwise match nothing.
/// </summary>
public static class DevDataSeeder
{
    public const string AdminUserName = "admin";
    public const string AdminPassword = "Admin123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new ApplicationRole(role));
        }

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByNameAsync(AdminUserName);
        if (admin is null)
        {
            admin = new ApplicationUser { UserName = AdminUserName, Email = "admin@ahk.aut.bme.hu", DisplayName = "Site Admin", EmailConfirmed = true };
            await userManager.CreateAsync(admin, AdminPassword);
            await userManager.AddToRoleAsync(admin, Roles.Admin);
        }

        // A non-admin instructor who is a member of exactly one course (used to verify course-scoping / 403s).
        var instructor = await userManager.FindByNameAsync("instructor");
        if (instructor is null)
        {
            instructor = new ApplicationUser { UserName = "instructor", Email = "instructor@ahk.aut.bme.hu", DisplayName = "Sample Instructor", EmailConfirmed = true };
            await userManager.CreateAsync(instructor, "Instructor123!");
        }

        // A course admin of that same course: no site role, but "Manage course" and the staff list are theirs.
        var courseAdmin = await userManager.FindByNameAsync("courseadmin");
        if (courseAdmin is null)
        {
            courseAdmin = new ApplicationUser { UserName = "courseadmin", Email = "courseadmin@ahk.aut.bme.hu", DisplayName = "Sample Course Admin", EmailConfirmed = true };
            await userManager.CreateAsync(courseAdmin, "CourseAdmin123!");
        }

        // The two courses are deliberately configured differently so the admin health dashboard shows a mix of
        // states in development: one fully wired up bar the access token, one with nothing filled in yet.
        var courseA = await EnsureCourseAsync(db, "viaubc01", "Sample Course VIAUBC01", "ahk-viaubc01", webhookSecret: "dev-webhook-secret");
        await EnsureCourseAsync(db, "viaubb01", "Sample Course VIAUBB01", "ahk-viaubb01", webhookSecret: null);

        // Both staff accounts are members of the first course only, in the two different course roles.
        await EnsureMembershipAsync(db, instructor.Id, courseA.Id, CourseRole.Instructor);
        await EnsureMembershipAsync(db, courseAdmin.Id, courseA.Id, CourseRole.Admin);

        await EnsureSampleDomainDataAsync(db, courseA);
        await EnsureSampleAssignmentsAsync(db, courseA);
    }

    private static async Task EnsureMembershipAsync(ApplicationDbContext db, int userId, int courseId, CourseRole role)
    {
        var exists = await db.CourseMemberships.IgnoreQueryFilters()
            .AnyAsync(m => m.UserId == userId && m.CourseId == courseId);

        if (exists)
            return;

        db.CourseMemberships.Add(new CourseMembership { UserId = userId, CourseId = courseId, Role = role });
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// One open and one archived assignment, so the instructor listing, the "show archived" toggle and the
    /// closed-invite branch all have something to render.
    /// </summary>
    private static async Task EnsureSampleAssignmentsAsync(ApplicationDbContext db, Course course)
    {
        if (await db.Assignments.IgnoreQueryFilters().AnyAsync(a => a.CourseId == course.Id))
            return;

        db.Assignments.AddRange(
            new Assignment
            {
                CourseId = course.Id,
                Name = "Homework 1 — Data access",
                Description = "Implement the repository layer and open a pull request from the solution branch.",
                TemplateRepoName = Normalize.RepoName($"{course.GitHubOrganization}/{course.Slug}-hw1"),
                InviteToken = $"dev-invite-{course.Slug}-hw1",
            },
            new Assignment
            {
                CourseId = course.Id,
                Name = "Homework 0 — Warm-up (archived)",
                Description = "Last semester's warm-up assignment. Archived: the invite link no longer accepts students.",
                TemplateRepoName = Normalize.RepoName($"{course.GitHubOrganization}/{course.Slug}-hw0"),
                InviteToken = $"dev-invite-{course.Slug}-hw0",
                ArchivedAt = DateTimeOffset.UtcNow.AddDays(-30),
            });

        await db.SaveChangesAsync();
    }

    private static async Task<Course> EnsureCourseAsync(ApplicationDbContext db, string slug, string name, string org, string? webhookSecret)
    {
        var course = await db.Courses.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Slug == slug);
        if (course is not null)
            return course;

        course = new Course
        {
            Slug = slug,
            Name = name,
            GitHubOrganization = org,
            RepoNamePrefix = slug,
            GitHubConfig = new CourseGitHubConfig
            {
                GitHubWebhookSecret = webhookSecret,
                WorkflowRunThreshold = 5,
                Enabled = true,
            },
        };

        db.Courses.Add(course);
        await db.SaveChangesAsync();

        db.CourseWebhookTokens.Add(new CourseWebhookToken
        {
            CourseId = course.Id,
            Token = $"dev-token-{slug}",
            Secret = $"dev-secret-{slug}",
            Description = "Development CI callback token",
        });
        await db.SaveChangesAsync();

        return course;
    }

    /// <summary>Two students with submissions, a realistic event stream, and both an automated and a confirmed grade.</summary>
    private static async Task EnsureSampleDomainDataAsync(ApplicationDbContext db, Course course)
    {
        if (await db.Submissions.IgnoreQueryFilters().AnyAsync(s => s.CourseId == course.Id))
            return;

        var samples = new[]
        {
            (Neptun: "ABC123", Repo: $"{course.GitHubOrganization}/{course.Slug}-hw1-abc123", Points: new[] { 2d, 3d }, Conclusion: "success"),
            (Neptun: "XYZ789", Repo: $"{course.GitHubOrganization}/{course.Slug}-hw1-xyz789", Points: new[] { 1d, 0d }, Conclusion: "failure"),
        };

        var now = DateTimeOffset.UtcNow;

        foreach (var (neptun, repo, points, conclusion) in samples)
        {
            var student = new Student { CourseId = course.Id, Neptun = Normalize.Neptun(neptun) };
            db.Students.Add(student);
            await db.SaveChangesAsync();

            var submission = new Submission
            {
                CourseId = course.Id,
                StudentId = student.Id,
                GitHubRepoName = Normalize.RepoName(repo),
                LastEventAt = now,
            };
            db.Submissions.Add(submission);
            await db.SaveChangesAsync();

            db.SubmissionEvents.AddRange(
                new RepositoryCreatedEvent { CourseId = course.Id, SubmissionId = submission.Id, Timestamp = now.AddDays(-7) },
                new BranchCreatedEvent { CourseId = course.Id, SubmissionId = submission.Id, Timestamp = now.AddDays(-6), Branch = "solution" },
                new PullRequestEvent
                {
                    CourseId = course.Id,
                    SubmissionId = submission.Id,
                    Timestamp = now.AddDays(-5),
                    Number = 1,
                    Action = "opened",
                    HtmlUrl = $"https://github.com/{submission.GitHubRepoName}/pull/1",
                    Neptun = student.Neptun,
                    Assignees = new List<string> { "instructor" },
                },
                new WorkflowRunEvent { CourseId = course.Id, SubmissionId = submission.Id, Timestamp = now.AddDays(-5), Conclusion = conclusion });

            // Automated evaluation result, then the teacher's confirmed grade (append-only history).
            AddGrade(db, course, submission, student, points, confirmed: false, date: now.AddDays(-5), actor: "grade-management-api", origin: $"https://github.com/{submission.GitHubRepoName}/commit/abc1234");
            AddGrade(db, course, submission, student, points, confirmed: true, date: now.AddDays(-1), actor: "instructor", origin: $"https://github.com/{submission.GitHubRepoName}/pull/1");

            await db.SaveChangesAsync();
        }
    }

    private static void AddGrade(ApplicationDbContext db, Course course, Submission submission, Student student, double[] points, bool confirmed, DateTimeOffset date, string actor, string origin)
    {
        var grade = new GradeRecord
        {
            CourseId = course.Id,
            SubmissionId = submission.Id,
            StudentId = student.Id,
            Neptun = student.Neptun,
            PrNumber = 1,
            PrUrl = $"https://github.com/{submission.GitHubRepoName}/pull/1",
            Date = date,
            Actor = actor,
            Origin = origin,
            Confirmed = confirmed,
        };

        for (var i = 0; i < points.Length; i++)
            grade.Points.Add(new GradeExercisePoint { Name = $"ex{i}", Point = points[i], Order = i });

        db.GradeRecords.Add(grade);
    }
}
