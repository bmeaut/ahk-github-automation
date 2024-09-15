using GradeManagement.Bll.Services;
using GradeManagement.Server.Authorization.Policies;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.Request;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[Route("api/subjects")]
[ApiController]
public class SubjectController(SubjectService subjectService)
    : CrudControllerBase<Subject, Shared.Dtos.Response.Subject>(subjectService)
{
    [HttpGet("{id:long}/courses")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<Course>> GetAllCoursesByIdAsync([FromRoute] long id)
    {
        return await subjectService.GetAllCoursesByIdAsync(id);
    }

    [HttpGet("{id:long}/teachers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<User>> GetAllTeachersByIdAsync([FromRoute] long id)
    {
        return await subjectService.GetAllTeachersByIdAsync(id);
    }

    [Authorize(Policy = TeacherRequirement.PolicyName)]
    [HttpPost]
    public override async Task<Shared.Dtos.Response.Subject> CreateAsync([FromBody] Subject requestDto)
    {
        return await base.CreateAsync(requestDto);
    }

    [HttpPost("{subjectId:long}/teachers/{teacherId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<User>> AddTeacherToSubjectByIdAsync([FromRoute] long subjectId, [FromRoute] long teacherId)
    {
        return await subjectService.AddTeacherToSubjectByIdAsync(subjectId, teacherId);
    }

    [HttpDelete("{subjectId:long}/teachers/{teacherId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> DeleteTeacherFromSubjectByIdAsync([FromRoute] long subjectId,
        [FromRoute] long teacherId)
    {
        await subjectService.DeleteTeacherFromSubjectByIdAsync(subjectId, teacherId);
        return NoContent();
    }
}
