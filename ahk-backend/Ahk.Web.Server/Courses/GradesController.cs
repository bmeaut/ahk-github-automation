using Ahk.Web.Data.Entities;
using Ahk.Web.Server.CourseContext;
using Ahk.Web.Services.Grading;
using Ahk.Web.Services.Grading.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ahk.Web.Server.Courses;

/// <summary>
/// Course-scoped final grades — the port of the legacy <c>list-grades/{*repoprefix}</c> function, with the
/// repository prefix replaced by the {course} route segment. The CSV endpoint preserves the original export
/// format so downstream administration keeps working.
/// </summary>
[ApiController]
[Route("api/{course}/grades")]
[Authorize(Policy = CourseMembershipRequirement.PolicyName)]
public sealed class GradesController : ControllerBase
{
    private readonly IGradeListingService gradeListing;

    public GradesController(IGradeListingService gradeListing) => this.gradeListing = gradeListing;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FinalStudentGrade>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FinalStudentGrade>>> List(CancellationToken cancellationToken)
    {
        var course = CurrentCourse();
        return Ok(await gradeListing.ListAsync(course.Id, cancellationToken));
    }

    [HttpGet("csv")]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCsv(CancellationToken cancellationToken)
    {
        var course = CurrentCourse();
        var csv = await gradeListing.ExportCsvAsync(course.Id, cancellationToken);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"{course.Slug}-grades.csv");
    }

    private Course CurrentCourse() => (Course)HttpContext.Items[CourseResolutionMiddleware.CourseItemKey]!;
}
