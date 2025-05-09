using GradeManagement.Client.Network;

namespace GradeManagement.Client.Components.Helpers;

public static class Extensions
{
    public static SubjectRequest ToRequest(this SubjectResponse subject, ICollection<User>? teachers = null)
    {
        return new SubjectRequest
        {
            Id = subject.Id,
            Name = subject.Name,
            NeptunCode = subject.NeptunCode,
            GitHubOrgName = subject.GitHubOrgName,
            Teachers = teachers,
        };
    }

    public static SubjectResponse ToResponse(this SubjectRequest subject)
    {
        return new SubjectResponse
        {
            Id = subject.Id,
            Name = subject.Name,
            NeptunCode = subject.NeptunCode,
            GitHubOrgName = subject.GitHubOrgName,
        };
    }

    public static GroupRequest ToRequest(this GroupResponse group, ICollection<User>? teachers = null)
    {
        return new GroupRequest { Id = group.Id, Name = group.Name, CourseId = group.CourseId, Teachers = teachers };
    }

    public static GroupResponse ToResponse(this GroupRequest group)
    {
        return new GroupResponse { Id = group.Id, Name = group.Name, CourseId = group.CourseId };
    }

    public static ExerciseRequest ToRequest(this ExerciseResponse exercise)
    {
        return new ExerciseRequest
        {
            Id = exercise.Id,
            CourseId = exercise.CourseId,
            Name = exercise.Name,
            DueDate = exercise.DueDate,
            GithubPrefix = exercise.GithubPrefix,
            ScoreTypes = exercise.ScoreTypes,
            ClassroomUrl = exercise.ClassroomUrl,
            MoodleScoreNamePrefix = exercise.MoodleScoreNamePrefix
        };
    }

    public static ExerciseResponse ToResponse(this ExerciseRequest exercise)
    {
        return new ExerciseResponse
        {
            Id = exercise.Id,
            CourseId = exercise.CourseId,
            Name = exercise.Name,
            DueDate = exercise.DueDate,
            GithubPrefix = exercise.GithubPrefix,
            ScoreTypes = exercise.ScoreTypes
        };
    }


    public static StudentRequest ToRequest(this StudentResponse student, ICollection<long>? groupIds = null)
    {
        return new StudentRequest
        {
            Id = student.Id,
            Name = student.Name,
            NeptunCode = student.NeptunCode,
            GithubId = student.GithubId,
            GroupIds = groupIds,
        };
    }

    public static StudentResponse ToResponse(this StudentRequest student)
    {
        return new StudentResponse
        {
            Id = student.Id, Name = student.Name, NeptunCode = student.NeptunCode, GithubId = student.GithubId,
        };
    }
}
