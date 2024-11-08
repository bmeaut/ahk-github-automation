using GradeManagement.Bll.Services;
using GradeManagement.Server.Authorization.Policies;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.Request;
using GradeManagement.Shared.Dtos.Response;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[Route("api/subjects")]
[ApiController]
public class SubjectController(SubjectService subjectService, IHttpContextAccessor httpContextAccessor)
    : CrudControllerBase<SubjectRequest, SubjectResponse>(subjectService)
{
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = TeacherOnSubjectRequirement.PolicyName)]
    public override async Task<SubjectResponse> UpdateAsync(long id, SubjectRequest requestDto) =>
        await base.UpdateAsync(id, requestDto);

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = TeacherRequirement.PolicyName)]
    public override async Task<SubjectResponse> CreateAsync(SubjectRequest requestDto) =>
        await base.CreateAsync(requestDto);

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = TeacherOnSubjectRequirement.PolicyName)]
    public override async Task<ActionResult> DeleteAsync(long id) => await base.DeleteAsync(id);

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public override async Task<IEnumerable<SubjectResponse>> GetAllAsync() =>
        await subjectService.GetAllAsyncForUserWithoutQf(HttpContext.User);

    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public override async Task<SubjectResponse> GetByIdAsync(long id) =>
        await base.GetByIdAsync(id);

    [HttpGet("{id:long}/courses")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
    public async Task<List<Course>> GetAllCoursesByIdAsync([FromRoute] long id)
    {
        return await subjectService.GetAllCoursesByIdAsync(id);
    }

    [HttpGet("{id:long}/teachers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
    public async Task<List<User>> GetAllTeachersByIdAsync([FromRoute] long id)
    {
        return await subjectService.GetAllTeachersByIdAsync(id);
    }

    [HttpPost("{subjectId:long}/teachers/{teacherId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = TeacherOnSubjectRequirement.PolicyName)]
    public async Task<List<User>> AddTeacherToSubjectByIdAsync([FromRoute] long subjectId, [FromRoute] long teacherId)
    {
        return await subjectService.AddTeacherToSubjectByIdAsync(subjectId, teacherId);
    }

    [HttpDelete("{subjectId:long}/teachers/{teacherId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = TeacherOnSubjectRequirement.PolicyName)]
    public async Task<ActionResult> DeleteTeacherFromSubjectByIdAsync([FromRoute] long subjectId,
        [FromRoute] long teacherId)
    {
        await subjectService.DeleteTeacherFromSubjectByIdAsync(subjectId, teacherId);
        return NoContent();
    }
}
