using GradeManagement.Bll;
using GradeManagement.Bll.Services;
using GradeManagement.Server.Authorization.Policies;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.Response;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[Authorize]
[Route("api/courses")]
[ApiController]
public class CourseController(CourseService courseService) : CrudControllerBase<Course>(courseService)
{
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = TeacherOnSubjectRequirement.PolicyName)]
    public override async Task<Course> UpdateAsync(long id, Course requestDto) => await base.UpdateAsync(id, requestDto);

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = TeacherOnSubjectRequirement.PolicyName)]
    public override async Task<Course> CreateAsync(Course requestDto) => await base.CreateAsync(requestDto);

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = TeacherOnSubjectRequirement.PolicyName)]
    public override async Task<ActionResult> DeleteAsync(long id) => await base.DeleteAsync(id);

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
    public override async Task<IEnumerable<Course>> GetAllAsync() => await base.GetAllAsync();

    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
    public override async Task<Course> GetByIdAsync(long id) => await base.GetByIdAsync(id);

    [HttpGet("{id:long}/exercises")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
    public async Task<IEnumerable<ExerciseResponse>> GetAllExercisesByIdAsync([FromRoute] long id)
    {
        return await courseService.GetAllExercisesByIdAsync(id);
    }

    [HttpGet("{id:long}/groups")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
    public async Task<IEnumerable<GroupResponse>> GetAllGroupsByIdAsync([FromRoute] long id)
    {
        return await courseService.GetAllGroupsByIdAsync(id);
    }
}
