using AutoMapper;

using GradeManagement.Data.Models;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.Response;

using Assignment = GradeManagement.Data.Models.Assignment;
using Course = GradeManagement.Data.Models.Course;
using Language = GradeManagement.Data.Models.Language;
using PullRequest = GradeManagement.Data.Models.PullRequest;
using Score = GradeManagement.Data.Models.Score;
using ScoreType = GradeManagement.Data.Models.ScoreType;
using ScoreTypeExercise = GradeManagement.Data.Models.ScoreTypeExercise;
using Semester = GradeManagement.Data.Models.Semester;
using User = GradeManagement.Data.Models.User;

namespace GradeManagement.Bll.Profiles;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Assignment, Shared.Dtos.Assignment>();
        CreateMap<Course, Shared.Dtos.Course>();
        CreateMap<Exercise, ExerciseResponse>().ForMember(dest => dest.ScoreTypes,
            opt => opt.MapFrom(src =>
                src.ScoreTypeExercises.Where(ste => ste.ExerciseId == src.Id)
                    .ToDictionary(ste => ste.Order, ste => ste.ScoreType.Type)));
        CreateMap<Group, GroupResponse>();
        CreateMap<Language, Shared.Dtos.Language>();
        CreateMap<PullRequest, Shared.Dtos.PullRequest>();
        CreateMap<PullRequest, PullRequestForDashboard>();
        CreateMap<Score, Shared.Dtos.Score>();
        CreateMap<ScoreType, Shared.Dtos.ScoreType>();
        CreateMap<ScoreTypeExercise, Shared.Dtos.ScoreTypeExercise>();
        CreateMap<Semester, Shared.Dtos.Semester>();
        CreateMap<Student, StudentResponse>();
        CreateMap<Subject, SubjectResponse>().ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.SubjectId));
        CreateMap<User, Shared.Dtos.User>();
    }
}
