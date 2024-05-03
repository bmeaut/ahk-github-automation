using GradeManagement.Client.Network;

namespace GradeManagement.Client.Components.Helpers;

public static class Extensions
{
    public static Subject2 ToSubject2(this Subject subject, ICollection<User>? teachers = null)
    {
        return new Subject2
        {
            Id = subject.Id,
            Name = subject.Name,
            NeptunCode = subject.NeptunCode,
            GitHubOrgName = subject.GitHubOrgName,
            Teachers = teachers
        };
    }

    public static Subject ToSubject(this Subject2 subject)
    {
        return new Subject
        {
            Id = subject.Id,
            Name = subject.Name,
            NeptunCode = subject.NeptunCode,
            GitHubOrgName = subject.GitHubOrgName
        };
    }

    public static Group2 ToGroup2(this Group group, ICollection<User>? teachers = null)
    {
        return new Group2 { Id = group.Id, Name = group.Name, CourseId = group.CourseId, Teachers = teachers };
    }

    public static Group ToGroup(this Group2 group)
    {
        return new Group { Id = group.Id, Name = group.Name, CourseId = group.CourseId };
    }

    public static Student2 ToStudent2(this Student student, ICollection<long>? groupIds = null)
    {
        return new Student2
        {
            Id = student.Id,
            Name = student.Name,
            NeptunCode = student.NeptunCode,
            GithubId = student.GithubId,
            GroupIds = groupIds
        };
    }

    public static Student ToStudent(this Student2 student)
    {
        return new Student
        {
            Id = student.Id, Name = student.Name, NeptunCode = student.NeptunCode, GithubId = student.GithubId
        };
    }
}
