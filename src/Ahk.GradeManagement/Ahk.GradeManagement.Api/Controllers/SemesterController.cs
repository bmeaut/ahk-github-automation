using GradeManagement.Bll;
using GradeManagement.Bll.Services;
using GradeManagement.Server.Authorization.Policies;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[Authorize]
[Route("api/semesters")]
[ApiController]
public class SemesterController(SemesterService semesterService)
    : RestrictedCrudControllerBase<Semester>(semesterService)
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = TeacherRequirement.PolicyName)]
    public override async Task<Semester> CreateAsync(Semester requestDto) => await base.CreateAsync(requestDto);

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = AdminRequirement.PolicyName)]
    public override async Task<ActionResult> DeleteAsync(long id) => await base.DeleteAsync(id);

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = UserRequirement.PolicyName)]
    public override async Task<IEnumerable<Semester>> GetAllAsync() => await base.GetAllAsync();

    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = UserRequirement.PolicyName)]
    public override async Task<Semester> GetByIdAsync(long id) => await base.GetByIdAsync(id);
}
