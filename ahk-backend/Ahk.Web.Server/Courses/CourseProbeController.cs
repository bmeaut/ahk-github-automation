using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Server.CourseContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Server.Courses;

/// <summary>
/// Course context (<c>/api/{course}/...</c>). Temporary endpoint that proves course-scoping end-to-end:
/// the <c>CourseMember</c> policy gates access, and reads/writes are automatically confined to the current
/// course by the EF query filter. Removed once real course endpoints land in the port.
/// </summary>
[ApiController]
[Route("api/{course}/probe")]
[Authorize(Policy = CourseMembershipRequirement.PolicyName)]
public sealed class CourseProbeController : ControllerBase
{
    private readonly ApplicationDbContext db;

    public CourseProbeController(ApplicationDbContext db) => this.db = db;

    /// <summary>Returns notes for the current course only (enforced by the global query filter).</summary>
    [HttpGet("notes")]
    [ProducesResponseType(typeof(IEnumerable<CourseNoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CourseNoteDto>>> GetNotes()
    {
        var notes = await db.CourseNotes
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new CourseNoteDto { Id = n.Id, Text = n.Text, CreatedAt = n.CreatedAt })
            .ToListAsync();

        return Ok(notes);
    }

    [HttpPost("notes")]
    [ProducesResponseType(typeof(CourseNoteDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CourseNoteDto>> AddNote([FromBody] CreateCourseNoteRequest request, [FromServices] ICurrentCourseProvider currentCourse)
    {
        var note = new CourseNote { CourseId = currentCourse.CurrentCourseId!.Value, Text = request.Text };
        db.CourseNotes.Add(note);
        await db.SaveChangesAsync();

        return Ok(new CourseNoteDto { Id = note.Id, Text = note.Text, CreatedAt = note.CreatedAt });
    }
}

public sealed class CourseNoteDto
{
    public Guid Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CreateCourseNoteRequest
{
    public string Text { get; set; } = string.Empty;
}
