using GradeManagement.Bll.Services;
using GradeManagement.Server.Authorization.Policies;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.Request;
using GradeManagement.Shared.Dtos.Response;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[Authorize]
[Route("api/students")]
[ApiController]
public class StudentController(StudentService studentService) : QueryControllerBase<StudentResponse>(studentService)
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = TeacherRequirement.PolicyName)]
    public override async Task<IEnumerable<StudentResponse>> GetAllAsync() => await base.GetAllAsync();

    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = TeacherRequirement.PolicyName)]
    public override async Task<StudentResponse> GetByIdAsync(long id) => await base.GetByIdAsync(id);

    [HttpGet("{id:long}/groups")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = TeacherRequirement.PolicyName)]
    public async Task<List<GroupResponse>> GetAllGroupsByIdAsync(long id)
    {
        return await studentService.GetAllGroupsByIdAsync(id);
    }

    [HttpGet("{id:long}/assignments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = TeacherRequirement.PolicyName)]
    public async Task<List<Assignment>> GetAllAssignmentsByIdAsync(long id)
    {
        return await studentService.GetAllAssignmentsByIdAsync(id);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = TeacherRequirement.PolicyName)]
    public async Task<StudentResponse> CreateAsync([FromBody] StudentRequest requestDto)
    {
        return await studentService.CreateAsync(requestDto);
    }
}
