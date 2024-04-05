using GradeManagement.Bll;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.Request;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[Route("api/subjects")]
[ApiController]
public class SubjectController(SubjectService subjectService) : CrudControllerBase<Subject, Shared.Dtos.Response.Subject>(subjectService)
{
    [HttpGet("{id:long}/courses")]
    public async Task<List<Course>> GetAllCoursesByIdAsync([FromRoute] long id)
    {
        return await subjectService.GetAllCoursesByIdAsync(id);
    }
    [HttpGet("{id:long}/teachers")]
    public async Task<List<User>> GetAllTeachersByIdAsync([FromRoute] long id)
    {
        return await subjectService.GetAllTeachersByIdAsync(id);
    }
}
