using AutoMapper;

using GradeManagement.Shared.Dtos;

using Assignment = GradeManagement.Shared.Dtos.Assignment;
using AssignmentEvent = GradeManagement.Shared.Dtos.AssignmentEvent;
using Course = GradeManagement.Shared.Dtos.Course;
using Exercise = GradeManagement.Shared.Dtos.Exercise;
using Group = GradeManagement.Shared.Dtos.Group;
using GroupStudent = GradeManagement.Shared.Dtos.GroupStudent;
using Language = GradeManagement.Shared.Dtos.Language;
using PullRequest = GradeManagement.Shared.Dtos.PullRequest;
using Score = GradeManagement.Shared.Dtos.Score;
using Semester = GradeManagement.Shared.Dtos.Semester;
using Student = GradeManagement.Shared.Dtos.Student;
using Subject = GradeManagement.Shared.Dtos.Response.Subject;
using Teacher = GradeManagement.Shared.Dtos.Teacher;

namespace GradeManagement.Server.Profiles;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Data.Models.Assignment, Assignment>();
        CreateMap<Data.Models.AssignmentEvent, AssignmentEvent>();
        CreateMap<Data.Models.Course, Course>();
        CreateMap<Data.Models.GroupTeacher, GroupTeacher>();
        CreateMap<Data.Models.Exercise, Exercise>();
        CreateMap<Data.Models.Group, Group>();
        CreateMap<Data.Models.GroupStudent, GroupStudent>();
        CreateMap<Data.Models.Language, Language>();
        CreateMap<Data.Models.PullRequest, PullRequest>();
        CreateMap<Data.Models.Score, Score>();
        CreateMap<Data.Models.Semester, Semester>();
        CreateMap<Data.Models.Student, Student>();
        CreateMap<Data.Models.Subject, Subject>();
        CreateMap<Data.Models.Teacher, Teacher>();
    }
}
