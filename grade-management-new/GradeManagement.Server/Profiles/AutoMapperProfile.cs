using AutoMapper;

using GradeManagement.Data.Models;

namespace GradeManagement.Server.Profiles;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Assignment, Shared.Dtos.Assignment>();
        CreateMap<AssignmentLog, Shared.Dtos.AssignmentLog>();
        CreateMap<Course, Shared.Dtos.Course>();
        CreateMap<Exercise, Shared.Dtos.Exercise>();
        CreateMap<Group, Shared.Dtos.Response.Group>();
        CreateMap<Language, Shared.Dtos.Language>();
        CreateMap<PullRequest, Shared.Dtos.PullRequest>();
        CreateMap<Score, Shared.Dtos.Score>();
        CreateMap<Semester, Shared.Dtos.Semester>();
        CreateMap<Student, Shared.Dtos.Response.Student>();
        CreateMap<Subject, Shared.Dtos.Response.Subject>();
        CreateMap<User, Shared.Dtos.User>();
    }
}
