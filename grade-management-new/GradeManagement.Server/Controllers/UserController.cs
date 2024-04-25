using GradeManagement.Bll;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.Response;

using Microsoft.AspNetCore.Mvc;


namespace GradeManagement.Server.Controllers;

[Route("api/users")]
[ApiController]
public class UserController(UserService userService) : CrudControllerBase<User>(userService)
{
    [HttpGet("{id:long}/groups")]
    public async Task<List<Group>> GetAllGroupsByIdAsync([FromRoute] long id)
    {
        return await userService.GetAllGroupsByIdAsync(id);
    }

    [HttpGet("{id:long}/subjects")]
    public async Task<List<Subject>> GetAllSubjectsByIdAsync([FromRoute] long id)
    {
        return await userService.GetAllSubjectsByIdAsync(id);
    }

    [HttpGet("{id:long}/pullrequests")]
    public async Task<List<PullRequest>> GetAllPullRequestsByIdAsync([FromRoute] long id)
    {
        return await userService.GetAllPullRequestsByIdAsync(id);
    }
}
