using GradeManagement.Bll.Services;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.Response;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[Authorize]
[Route("api/users")]
[ApiController]
public class UserController(UserService userService) : CrudControllerBase<User>(userService)
{
    public override async Task<User> UpdateAsync(long id, User requestDto) => await base.UpdateAsync(id, requestDto);

    public override async Task<User> CreateAsync(User requestDto) => await base.CreateAsync(requestDto);

    public override async Task<ActionResult> DeleteAsync(long id) => await base.DeleteAsync(id);

    public override async Task<IEnumerable<User>> GetAllAsync() => await base.GetAllAsync();

    public override async Task<User> GetByIdAsync(long id) => await base.GetByIdAsync(id);

    [HttpGet("{id:long}/groups")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<GroupResponse>> GetAllGroupsByIdAsync([FromRoute] long id)
    {
        return await userService.GetAllGroupsByIdAsync(id);
    }

    [HttpGet("{id:long}/subjects")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<SubjectResponse>> GetAllSubjectsByIdAsync([FromRoute] long id)
    {
        return await userService.GetAllSubjectsByIdAsync(id);
    }

    [HttpGet("{id:long}/pullrequests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<PullRequest>> GetAllPullRequestsByIdAsync([FromRoute] long id)
    {
        return await userService.GetAllPullRequestsByIdAsync(id);
    }
}
