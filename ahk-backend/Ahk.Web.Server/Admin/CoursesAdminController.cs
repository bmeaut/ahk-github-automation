using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Admin.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Server.Admin;

/// <summary>
/// Host/admin context (no {course} segment): site super-admins manage the set of courses and, later, their
/// connected GitHub environments. This centralizes what used to be a separate per-course Azure deployment.
/// </summary>
[ApiController]
[Route("api/admin/courses")]
[Authorize(Roles = Roles.Admin)]
public sealed class CoursesAdminController : ControllerBase
{
    private readonly ApplicationDbContext db;

    public CoursesAdminController(ApplicationDbContext db) => this.db = db;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CourseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CourseDto>>> List()
    {
        var courses = await db.Courses
            .AsNoTracking()
            .OrderBy(c => c.Slug)
            .Select(c => new CourseDto
            {
                Id = c.Id,
                Slug = c.Slug,
                Name = c.Name,
                GitHubOrganization = c.GitHubOrganization,
                CreatedAt = c.CreatedAt,
            })
            .ToListAsync();

        return Ok(courses);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseDto>> Get(Guid id)
    {
        var course = await db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return course is null ? NotFound() : Ok(ToDto(course));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseDto>> Create([FromBody] CreateCourseRequest request)
    {
        if (await db.Courses.AnyAsync(c => c.Slug == request.Slug))
            return Conflict(new { error = $"A course with slug '{request.Slug}' already exists." });

        var course = new Course { Slug = request.Slug, Name = request.Name, GitHubOrganization = request.GitHubOrganization };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = course.Id }, ToDto(course));
    }

    private static CourseDto ToDto(Course c) => new()
    {
        Id = c.Id,
        Slug = c.Slug,
        Name = c.Name,
        GitHubOrganization = c.GitHubOrganization,
        CreatedAt = c.CreatedAt,
    };
}
