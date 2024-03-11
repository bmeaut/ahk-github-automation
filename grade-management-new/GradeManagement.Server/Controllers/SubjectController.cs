using GradeManagement.Bll;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[Route("api/subjects")]
[ApiController]
public class SubjectController(SubjectService subjectService) : CrudControllerBase<Subject>(subjectService)
{
    [HttpGet("{id:long}/courses")]
    public async Task<List<Course>> GetAllCoursesByIdAsync([FromRoute] long id)
    {
        return await subjectService.GetAllCoursesByIdAsync(id);
    }
}
