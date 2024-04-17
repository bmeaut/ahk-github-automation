using GradeManagement.Bll;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.Response;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[Route("api/students")]
[ApiController]
public class StudentController(StudentService studentService) : QueryControllerBase<Student>(studentService)
{
    [HttpGet("{id:long}/groups")]
    public async Task<List<Group>> GetAllGroupsByIdAsync(long id)
    {
        return await studentService.GetAllGroupsByIdAsync(id);
    }

    [HttpGet("{id:long}/assignments")]
    public async Task<List<Assignment>> GetAllAssignmentsByIdAsync(long id)
    {
        return await studentService.GetAllAssignmentsByIdAsync(id);
    }
}
