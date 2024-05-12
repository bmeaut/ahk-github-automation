using GradeManagement.Bll;
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
    [HttpGet("{id:long}/exercises")]
    public async Task<IEnumerable<Exercise>> GetAllExercisesByIdAsync([FromRoute] long id)
    {
        return await courseService.GetAllExercisesByIdAsync(id);
    }

    [HttpGet("{id:long}/groups")]
    public async Task<IEnumerable<Group>> GetAllGroupsByIdAsync([FromRoute] long id)
    {
        return await courseService.GetAllGroupsByIdAsync(id);
    }
}
