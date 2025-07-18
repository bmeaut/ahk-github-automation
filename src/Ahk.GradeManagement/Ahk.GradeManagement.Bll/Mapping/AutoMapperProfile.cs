using Ahk.GradeManagement.Dal.Entities;
using Ahk.GradeManagement.Shared.Dtos;
using Ahk.GradeManagement.Shared.Dtos.Response;

using AutoMapper;

using Assignment = Ahk.GradeManagement.Dal.Entities.Assignment;
using Course = Ahk.GradeManagement.Dal.Entities.Course;
using Language = Ahk.GradeManagement.Dal.Entities.Language;
using PullRequest = Ahk.GradeManagement.Dal.Entities.PullRequest;
using Score = Ahk.GradeManagement.Dal.Entities.Score;
using ScoreType = Ahk.GradeManagement.Dal.Entities.ScoreType;
using ScoreTypeExercise = Ahk.GradeManagement.Dal.Entities.ScoreTypeExercise;
using Semester = Ahk.GradeManagement.Dal.Entities.Semester;
using User = Ahk.GradeManagement.Dal.Entities.User;

namespace Ahk.GradeManagement.Bll.Profiles;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Assignment, Shared.Dtos.Assignment>();
        CreateMap<Course, CourseResponse>();
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
