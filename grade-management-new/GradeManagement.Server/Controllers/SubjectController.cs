using GradeManagement.Bll;
using GradeManagement.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers
{
    [Route("api/subjects")]
    [ApiController]
    public class SubjectController : ControllerBase
    {
        private readonly SubjectService _subjectService;

        public SubjectController(SubjectService subjectService)
        {
            _subjectService = subjectService;
        }

        [HttpGet]
        public async Task<IEnumerable<Subject>> GetSubjectsAsync()
        {
            return await _subjectService.GetAllSubjectsAsync();
        }

        [HttpGet("{id}")]
        public async Task<Subject> GetSubjectByIdAsync([FromRoute] long id)
        {
            return await _subjectService.GetSubjectByIdAsync(id);
        }

        [HttpPut("{id}")]
        public async Task<Subject> UpdateSubjectAsync([FromRoute] long id, [FromBody] Subject subject)
        {
            return await _subjectService.UpdateSubjectAsync(id, subject);
        }

        [HttpPost]
        public async Task<Subject> CreateSubjectAsync([FromBody] Subject subject)
        {
            return await _subjectService.CreateSubjectAsync(subject);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSubjectAsync([FromRoute] long id)
        {
            await _subjectService.DeleteSubjectAsync(id);
            return this.NoContent();
        }
    }
}
