using GradeManagement.Bll;
using GradeManagement.Bll.Services;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;

using Microsoft.AspNetCore.Mvc;

using System.Text;

namespace GradeManagement.Server.Controllers;

[Route("api/exercises")]
[ApiController]
public class ExerciseController(ExerciseService exerciseService) : CrudControllerBase<Exercise>(exerciseService)
{
    [HttpGet("{id:long}/assignments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IEnumerable<Assignment>> GetAssignmentsByIdAsync([FromRoute] long id)
    {
        return await exerciseService.GetAssignmentsByIdAsync(id);
    }

    [HttpGet("{id:long}/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<FileContentResult> ExportToCsvAsync([FromRoute] long id)
    {
        var csv = await exerciseService.GetCsvByExerciseId(id);
        return new FileContentResult(Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8");
    }
}
