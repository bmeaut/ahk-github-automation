using GradeManagement.Bll;
using GradeManagement.Bll.Services;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.Request;

using Microsoft.AspNetCore.Mvc;

using Student = GradeManagement.Shared.Dtos.Response.Student;

namespace GradeManagement.Server.Controllers
{
    [Route("api/groups")]
    [ApiController]
    public class GroupController(GroupService groupService) : CrudControllerBase<Group,Shared.Dtos.Response.Group>(groupService)
    {
        [HttpGet("{id:long}/teachers")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<List<User>> GetAllTeachersByIdAsync([FromRoute] long id)
        {
            return await groupService.GetAllTeachersByIdAsync(id);
        }

        [HttpGet("{id:long}/students")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<List<Student>> GetAllStudentsByIdAsync([FromRoute] long id)
        {
            return await groupService.GetAllStudentsByIdAsync(id);
        }
    }
}
