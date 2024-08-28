using AutoMapper;

using GradeManagement.Data.Models;

namespace GradeManagement.Bll.Profiles;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Assignment, Shared.Dtos.Assignment>();
        CreateMap<Course, Shared.Dtos.Course>();
        CreateMap<Exercise, Shared.Dtos.Response.Exercise>();
        CreateMap<Group, Shared.Dtos.Response.Group>();
        CreateMap<Language, Shared.Dtos.Language>();
        CreateMap<PullRequest, Shared.Dtos.PullRequest>();
        CreateMap<PullRequest, Shared.Dtos.PullRequestForDashboard>();
        CreateMap<Score, Shared.Dtos.Score>();
        CreateMap<ScoreType, Shared.Dtos.ScoreType>();
        CreateMap<ScoreTypeExercise, Shared.Dtos.ScoreTypeExercise>();
        CreateMap<Semester, Shared.Dtos.Semester>();
        CreateMap<Student, Shared.Dtos.Response.Student>();
        CreateMap<Subject, Shared.Dtos.Response.Subject>();
        CreateMap<User, Shared.Dtos.User>();
    }
}
