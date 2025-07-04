using Ahk.GradeManagement.Api.Authorization.Policies;
using Ahk.GradeManagement.Api.Controllers.BaseControllers;
using Ahk.GradeManagement.Bll.Services;
using Ahk.GradeManagement.Shared.Dtos;
using Ahk.GradeManagement.Shared.Dtos.Response;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

namespace Ahk.GradeManagement.Api.Controllers;

[Authorize]
[Route("api/users")]
[ApiController]
public class UserController(UserService userService, IHttpContextAccessor httpContextAccessor) : CrudControllerBase<User>(userService)
{
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = TeacherRequirement.PolicyName)]
    public override async Task<User> UpdateAsync(long id, User requestDto) => await base.UpdateAsync(id, requestDto);

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = TeacherRequirement.PolicyName)]
    public override async Task<User> CreateAsync(User requestDto) => await base.CreateAsync(requestDto);

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = AdminRequirement.PolicyName)]
    public override async Task<ActionResult> DeleteAsync(long id) => await base.DeleteAsync(id);

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = TeacherRequirement.PolicyName)]
    public override async Task<IEnumerable<User>> GetAllAsync() => await base.GetAllAsync();

    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = TeacherRequirement.PolicyName)]
    public override async Task<User> GetByIdAsync(long id) => await base.GetByIdAsync(id);

    [HttpGet("{id:long}/groups")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = TeacherRequirement.PolicyName)]
    public async Task<List<GroupResponse>> GetAllGroupsByIdAsync([FromRoute] long id)
    {
        return await userService.GetAllGroupsByIdAsync(id);
    }

    [HttpGet("{id:long}/subjects")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = TeacherRequirement.PolicyName)]
    public async Task<List<SubjectResponse>> GetAllSubjectsByIdAsync([FromRoute] long id)
    {
        return await userService.GetAllSubjectsByIdAsync(id);
    }

    [HttpGet("{id:long}/pullrequests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = TeacherRequirement.PolicyName)]
    public async Task<List<PullRequest>> GetAllPullRequestsByIdAsync([FromRoute] long id)
    {
        return await userService.GetAllPullRequestsByIdAsync(id);
    }

    [HttpGet("/me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<User> GetCurrentUserAsync() => await userService.GetCurrentUserAsync(httpContextAccessor.HttpContext.User);
}
