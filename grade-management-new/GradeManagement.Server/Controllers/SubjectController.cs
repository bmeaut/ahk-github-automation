using GradeManagement.Services.Services;
using GradeManagement.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers
{
    [Route("api/subjects")]
    [ApiController]
    public class SubjectController : ControllerBase
    {
        SubjectService _subjectService;

        public SubjectController(SubjectService subjectService)
        {
            _subjectService = subjectService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubjectDTO>>> GetSubjects()
        {
            return this.Ok(await _subjectService.GetAllSubjects());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SubjectDTO>> GetSubjectById([FromRoute] long id)
        {
            try
            {
                return this.Ok(await _subjectService.GetSubjectById(id));
            }
            catch (NullReferenceException e)
            {
                return this.NotFound(e.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<SubjectDTO>> UpdateSubject([FromRoute] long id, [FromBody] SubjectDTO subjectDto)
        {
            try
            {
                return this.Ok(await _subjectService.UpdateSubject(id, subjectDto));
            }
            catch (ArgumentException e)
            {
                return this.BadRequest(e.Message);
            }
            catch (NullReferenceException e)
            {
                return this.NotFound(e.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult<SubjectDTO>> CreateSubject([FromBody] SubjectDTO subjectDto)
        {
            return this.Ok(await _subjectService.CreateSubject(subjectDto));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSubject([FromRoute] long id)
        {
            try
            {
                await _subjectService.DeleteSubject(id);
                return this.NoContent();
            }
            catch (NullReferenceException e)
            {
                return this.NotFound(e.Message);
            }
        }
    }
}
