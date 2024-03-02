using AutoMapper;
using GradeManagement.Data.Data;
using GradeManagement.Data.Models;
using GradeManagement.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace GradeManagement.Services.Services;

public class SubjectService
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;
    private readonly IMapper _mapper;

    public SubjectService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<SubjectDTO>> GetAllSubjects()
    {
        List<Subject> allSubjects = await _gradeManagementDbContext.Subject.OrderByDescending(s => s.Id).ToListAsync();
        var subjectDtos = _mapper.Map<List<SubjectDTO>>(allSubjects);
        return subjectDtos;
    }

    public async Task<SubjectDTO> GetSubjectById(long id)
    {
        Subject? subject = await _gradeManagementDbContext.Subject.SingleOrDefaultAsync(s => s.Id == id);
        if (subject == null) throw new NullReferenceException("No subject could be found with the given Id!");
        var subjectDto = _mapper.Map<SubjectDTO>(subject);
        return subjectDto;
    }

    public async Task<SubjectDTO> UpdateSubject(long id, SubjectDTO subjectDto)
    {
        if (subjectDto.Id != id) throw new ArgumentException("The Id from the query and the Id of the DTO do not match!");
        Subject? subject = await _gradeManagementDbContext.Subject.Include(s=>s.Courses).SingleOrDefaultAsync(s => s.Id == id);
        if (subject == null) throw new NullReferenceException("No subject could be found with the given Id!");
        subject.Name = subjectDto.Name;
        subject.NeptunSubjectCode = subjectDto.NeptunSubjectCode;

        var courses = _gradeManagementDbContext.Course
            .Where(c => subjectDto.CourseDtos.Select(co => co.Id).Contains(c.Id)).ToList();
        List<Course> coursesToDelete = new List<Course>();
        foreach (var course in subject.Courses)
        {
            if (courses.All(c => c.Id != course.Id)) coursesToDelete.Add(course);
        }

        foreach (var course in coursesToDelete) subject.Courses.RemoveAll(c => c.Id == course.Id);
        foreach (var course in courses)
        {
            if (subject.Courses.All(c => c.Id != course.Id)) subject.Courses.Add(course);
        }

        await _gradeManagementDbContext.SaveChangesAsync();

        return _mapper.Map<SubjectDTO>(subject);
    }

    public async Task<SubjectDTO> CreateSubject(SubjectDTO subjectDto)
    {
        Subject subject = _mapper.Map<Subject>(subjectDto);
        subject.Courses = _gradeManagementDbContext.Course
            .Where(c => subjectDto.CourseDtos.Select(co => co.Id).Contains(c.Id)).ToList();
        await _gradeManagementDbContext.Subject.AddAsync(subject);
        await _gradeManagementDbContext.SaveChangesAsync();
        return _mapper.Map<SubjectDTO>(subject);
    }

    public async Task DeleteSubject(long id)
    {
        Subject? subject = await _gradeManagementDbContext.Subject.SingleOrDefaultAsync(s => s.Id == id);
        if (subject == null) throw new NullReferenceException("No subject could be found with the given Id!");
        _gradeManagementDbContext.Subject.Remove(subject);
        await _gradeManagementDbContext.SaveChangesAsync();
    }
}
