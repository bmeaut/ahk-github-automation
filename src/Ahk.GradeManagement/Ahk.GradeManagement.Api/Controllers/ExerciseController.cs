using Ahk.GradeManagement.Api.Authorization.Policies;
using Ahk.GradeManagement.Api.Controllers.BaseControllers;
using Ahk.GradeManagement.Bll.Services;
using Ahk.GradeManagement.Shared.Dtos;
using Ahk.GradeManagement.Shared.Dtos.Request;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ahk.GradeManagement.Api.Controllers;

[Authorize]
[Route("api/exercises")]
[ApiController]
public class ExerciseController(ExerciseService exerciseService)
    : CrudControllerBase<ExerciseRequest, Shared.Dtos.Response.ExerciseResponse>(exerciseService)
{
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = TeacherOnSubjectRequirement.PolicyName)]
    public override async Task<Shared.Dtos.Response.ExerciseResponse>
        UpdateAsync(long id, ExerciseRequest requestDto) => await base.UpdateAsync(id, requestDto);

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = TeacherOnSubjectRequirement.PolicyName)]
    public override async Task<Shared.Dtos.Response.ExerciseResponse> CreateAsync(ExerciseRequest requestDto) =>
        await base.CreateAsync(requestDto);

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = TeacherOnSubjectRequirement.PolicyName)]
    public override async Task<ActionResult> DeleteAsync(long id) => await base.DeleteAsync(id);

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
    public override async Task<IEnumerable<Shared.Dtos.Response.ExerciseResponse>> GetAllAsync() =>
        await base.GetAllAsync();

    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
    public override async Task<Shared.Dtos.Response.ExerciseResponse> GetByIdAsync(long id) =>
        await base.GetByIdAsync(id);

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
}
