using GradeManagement.Bll;
using GradeManagement.Bll.BaseServices;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers
{
    [Route("api/groups")]
    [ApiController]
    public class GroupController(GroupService groupService) : CrudControllerBase<Group>(groupService)
    {
        [HttpGet("{id:long}/teachers")]
        public async Task<List<User>> GetAllTeachersByIdAsync([FromRoute] long id)
        {
            return await groupService.GetAllTeachersByIdAsync(id);
        }
        [HttpGet("{id:long}/students")]
        public async Task<List<User>> GetAllStudentsByIdAsync([FromRoute] long id)
        {
            return await groupService.GetAllStudentsByIdAsync(id);
        }
    }
}
