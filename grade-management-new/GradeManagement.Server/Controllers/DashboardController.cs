using GradeManagement.Bll;
using GradeManagement.Shared.Dtos;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(DashboardService dashboardService) : ControllerBase
{
    [HttpGet("{subjectId:long}")]
    public async Task<ActionResult<List<Dashboard>>> GetDashboard(long subjectId)
    {
        var dashboard = await dashboardService.GetAllDashboardDtosForSubjectAsync(subjectId);
        return Ok(dashboard);
    }
}
