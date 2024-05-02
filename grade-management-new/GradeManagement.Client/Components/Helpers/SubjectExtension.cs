using GradeManagement.Client.Network;

namespace GradeManagement.Client.Components.Helpers;

public static class Extensions
{
    public static Subject2 ToSubject2(this Subject subject)
    {
        return new Subject2 { Id = subject.Id, Name = subject.Name, NeptunCode = subject.NeptunCode };
    }

    public static Subject ToSubject(this Subject2 subject)
    {
        return new Subject { Id = subject.Id, Name = subject.Name, NeptunCode = subject.NeptunCode };
    }

    public static Group2 ToGroup2(this Group group)
    {
        return new Group2 { Id = group.Id, Name = group.Name, CourseId = group.CourseId };
    }

    public static Group ToGroup(this Group2 group)
    {
        return new Group { Id = group.Id, Name = group.Name, CourseId = group.CourseId };
    }

    public static Student2 ToStudent2(this Student student)
    {
        return new Student2
        {
            Id = student.Id, Name = student.Name, NeptunCode = student.NeptunCode, GithubId = student.GithubId
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
