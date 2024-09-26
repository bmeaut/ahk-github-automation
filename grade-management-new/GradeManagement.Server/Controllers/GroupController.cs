using GradeManagement.Bll;
using GradeManagement.Bll.Services;
using GradeManagement.Server.Authorization.Policies;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.Request;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Student = GradeManagement.Shared.Dtos.Response.Student;

namespace GradeManagement.Server.Controllers
{
    [Authorize]
    [Route("api/groups")]
    [ApiController]
    public class GroupController(GroupService groupService) : CrudControllerBase<Group,Shared.Dtos.Response.Group>(groupService)
    {
        [HttpPut("{id:long}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Authorize(Policy = TeacherOnSubjectRequirement.PolicyName)]
        public override async Task<Shared.Dtos.Response.Group> UpdateAsync(long id, Group requestDto) => await base.UpdateAsync(id, requestDto);

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Authorize(Policy = TeacherOnSubjectRequirement.PolicyName)]
        public override async Task<Shared.Dtos.Response.Group> CreateAsync(Group requestDto) => await base.CreateAsync(requestDto);

        [HttpDelete("{id:long}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [Authorize(Policy = TeacherOnSubjectRequirement.PolicyName)]
        public override async Task<ActionResult> DeleteAsync(long id) => await base.DeleteAsync(id);

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
        public override async Task<IEnumerable<Shared.Dtos.Response.Group>> GetAllAsync() => await base.GetAllAsync();

        [HttpGet("{id:long}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
        public override async Task<Shared.Dtos.Response.Group> GetByIdAsync(long id) => await base.GetByIdAsync(id);

        [HttpGet("{id:long}/teachers")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
        public async Task<List<User>> GetAllTeachersByIdAsync([FromRoute] long id)
        {
            return await groupService.GetAllTeachersByIdAsync(id);
        }

        [HttpGet("{id:long}/students")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
        public async Task<List<Student>> GetAllStudentsByIdAsync([FromRoute] long id)
        {
            return await groupService.GetAllStudentsByIdAsync(id);
        }
    }
}
