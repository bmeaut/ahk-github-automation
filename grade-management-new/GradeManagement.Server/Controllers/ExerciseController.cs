using GradeManagement.Bll;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[Authorize]
[Route("api/exercises")]
[ApiController]
public class ExerciseController(ExerciseService exerciseService) : CrudControllerBase<Exercise>(exerciseService)
{
    [HttpGet("{id:long}/assignments")]
    public async Task<IEnumerable<Assignment>> GetAssignmentsByIdAsync([FromRoute] long id)
    {
        return await exerciseService.GetAssignmentsByIdAsync(id);
    }
    [HttpGet("{id:long}/export")]
    public async Task<FileContentResult> ExportToCsvAsync([FromRoute] long id)
    {
        return await exerciseService.GetCsvByExerciseId(id);
    }
}
