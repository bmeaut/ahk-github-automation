using GradeManagement.Bll;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[Route("api/exercises")]
[ApiController]
public class ExerciseController(ExerciseService exerciseService) : CrudControllerBase<Exercise>(exerciseService)
{
    [HttpGet("{id:long}/assignments")]
    public async Task<IEnumerable<Assignment>> GetAssignmentsByIdAsync(long id)
    {
        return await exerciseService.GetAssignmentsByIdAsync(id);
    }
}
