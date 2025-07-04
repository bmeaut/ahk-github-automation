using GradeManagement.Bll.Services;
using GradeManagement.Server.Authorization.Policies;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.Request;
using GradeManagement.Shared.Dtos.Response;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers
{
    [Authorize]
    [Route("api/groups")]
    [ApiController]
    public class GroupController(GroupService groupService)
        : CrudControllerBase<GroupRequest, GroupResponse>(groupService)
    {
        [HttpPut("{id:long}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Authorize(Policy = TeacherOnSubjectRequirement.PolicyName)]
        public override async Task<GroupResponse> UpdateAsync(long id, GroupRequest requestDto) =>
            await base.UpdateAsync(id, requestDto);

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Authorize(Policy = TeacherOnSubjectRequirement.PolicyName)]
        public override async Task<GroupResponse> CreateAsync(GroupRequest requestDto) =>
            await base.CreateAsync(requestDto);

        [HttpDelete("{id:long}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [Authorize(Policy = TeacherOnSubjectRequirement.PolicyName)]
        public override async Task<ActionResult> DeleteAsync(long id) => await base.DeleteAsync(id);

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
        public override async Task<IEnumerable<GroupResponse>> GetAllAsync() =>
            await base.GetAllAsync();

        [HttpGet("{id:long}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
        public override async Task<GroupResponse> GetByIdAsync(long id) =>
            await base.GetByIdAsync(id);

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
        public async Task<List<StudentResponse>> GetAllStudentsByIdAsync([FromRoute] long id)
        {
            return await groupService.GetAllStudentsByIdAsync(id);
        }
    }
}
