using GradeManagement.Bll;
using GradeManagement.Bll.Services;
using GradeManagement.Server.Authorization.Policies;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.Request;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Text;

namespace GradeManagement.Server.Controllers;

[Authorize]
[Route("api/exercises")]
[ApiController]
public class ExerciseController(ExerciseService exerciseService) : CrudControllerBase<Exercise, Shared.Dtos.Response.Exercise>(exerciseService)
{
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = TeacherOnSubjectRequirement.PolicyName)]
    public override async Task<Shared.Dtos.Response.Exercise> UpdateAsync(long id, Exercise requestDto) => await base.UpdateAsync(id, requestDto);

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = TeacherOnSubjectRequirement.PolicyName)]
    public override async Task<Shared.Dtos.Response.Exercise> CreateAsync(Exercise requestDto) => await base.CreateAsync(requestDto);

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = TeacherOnSubjectRequirement.PolicyName)]
    public override async Task<ActionResult> DeleteAsync(long id) => await base.DeleteAsync(id);

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
    public override async Task<IEnumerable<Shared.Dtos.Response.Exercise>> GetAllAsync() => await base.GetAllAsync();

    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
    public override async Task<Shared.Dtos.Response.Exercise> GetByIdAsync(long id) => await base.GetByIdAsync(id);

    [HttpGet("{id:long}/assignments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
    public async Task<IEnumerable<Assignment>> GetAssignmentsByIdAsync([FromRoute] long id)
    {
        return await exerciseService.GetAssignmentsByIdAsync(id);
    }

    [HttpGet("{id:long}/scoreTypes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
    public async Task<IEnumerable<ScoreTypeExercise>> ScoreTypeExercises([FromRoute] long id)
    {
        return await exerciseService.GetScoreTypeExercisesByIdAsync(id);
    }


    [HttpGet("{id:long}/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
    public async Task<FileContentResult> ExportToCsvAsync([FromRoute] long id)
    {
        var csv = await exerciseService.GetCsvByExerciseId(id);
        return new FileContentResult(Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8");
    }
}
