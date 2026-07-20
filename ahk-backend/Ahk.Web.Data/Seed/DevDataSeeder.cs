using Ahk.Web.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ahk.Web.Data.Seed;

/// <summary>
/// Seeds development data: applies migrations, ensures roles, a super-admin, two sample courses, a membership
/// in only one of them, and a probe note per course — enough to exercise auth and course-scoping locally.
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

        var seedCourses = new[]
        {
            new Course { Slug = "viaubc01", Name = "Sample Course VIAUBC01" },
            new Course { Slug = "viaubb01", Name = "Sample Course VIAUBB01" },
        };

        foreach (var course in seedCourses)
        {
            var existing = await db.Courses.FirstOrDefaultAsync(c => c.Slug == course.Slug);
            if (existing is null)
            {
                db.Courses.Add(course);
                db.CourseNotes.Add(new CourseNote { CourseId = course.Id, Text = $"Probe note for {course.Slug}" });
            }
        }

        await db.SaveChangesAsync();

        // Instructor is a member of the first course only.
        var firstCourse = await db.Courses.FirstAsync(c => c.Slug == "viaubc01");
        var hasMembership = await db.CourseMemberships.AnyAsync(m => m.UserId == instructor.Id && m.CourseId == firstCourse.Id);
        if (!hasMembership)
        {
            db.CourseMemberships.Add(new CourseMembership { UserId = instructor.Id, CourseId = firstCourse.Id, Role = CourseRole.Instructor });
            await db.SaveChangesAsync();
        }
    }
}
