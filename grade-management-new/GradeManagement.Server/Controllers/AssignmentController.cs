using GradeManagement.Bll;
using GradeManagement.Bll.BaseServices;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[Authorize]
[Route("api/assignments")]
[ApiController]
public class AssignmentController(AssignmentService assignmentService)
    : QueryControllerBase<Assignment>(assignmentService)
{
    [HttpGet("{id:long}/pullrequests")]
    public async Task<IEnumerable<PullRequest>> GetAllPullRequestsByIdAsync([FromRoute] long id)
    {
        return await assignmentService.GetAllPullRequestsByIdAsync(id);
    }
}
