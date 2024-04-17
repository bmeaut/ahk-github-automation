using GradeManagement.Bll;
using GradeManagement.Bll.BaseServices;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[Route("api/assignments")]
[ApiController]
public class AssignmentController(AssignmentService assignmentService)
    : QueryControllerBase<Assignment>(assignmentService)
{
    [HttpGet("{id:long}/scores")]
    public async Task<IEnumerable<Score>> GetAllScoresByIdAsync([FromRoute] long id)
    {
        return await assignmentService.GetAllScoresByIdAsync(id);
    }

    [HttpGet("{id:long}/pullrequests")]
    public async Task<IEnumerable<PullRequest>> GetAllPullRequestsByIdAsync([FromRoute] long id)
    {
        return await assignmentService.GetAllPullRequestsByIdAsync(id);
    }
}
