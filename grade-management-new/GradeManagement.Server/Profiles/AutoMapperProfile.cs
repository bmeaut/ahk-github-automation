using AutoMapper;
using GradeManagement.Data.Models;
using GradeManagement.Shared.DTOs;

namespace GradeManagement.Server.Profiles;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Assignment, AssignmentDTO>();
        CreateMap<AssignmentDTO, Assignment>();
        CreateMap<AssignmentEvent, AssignmentEventDTO>();
        CreateMap<AssignmentEventDTO, AssignmentEvent>();
        CreateMap<Course, CourseDTO>();
        CreateMap<CourseDTO, Course>();
        CreateMap<Group, GroupDTO>();
        CreateMap<GroupDTO, Group>();
        CreateMap<Language, LanguageDTO>();
        CreateMap<LanguageDTO, Language>();
        CreateMap<PullRequest, PullRequestDTO>();
        CreateMap<PullRequestDTO, PullRequest>();
        CreateMap<Score, ScoreDTO>();
        CreateMap<ScoreDTO, Score>();
        CreateMap<Semester, SemesterDTO>();
        CreateMap<SemesterDTO, Semester>();
        CreateMap<Student, StudentDTO>();
        CreateMap<StudentDTO, Student>();
        CreateMap<Subject, SubjectDTO>();
        CreateMap<SubjectDTO, Subject>()
            .ForMember(m => m.Courses, opt => opt.Ignore());
        CreateMap<Data.Models.Task, TaskDTO>();
        CreateMap<TaskDTO, Data.Models.Task>();
    }
}
