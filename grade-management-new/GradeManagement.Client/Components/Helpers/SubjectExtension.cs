using GradeManagement.Client.Network;

namespace GradeManagement.Client.Components.Helpers;

public static class SubjectExtensions
{
    public static Subject2 ToSubject2(this Subject subject)
    {
        return new Subject2 { Id = subject.Id, Name = subject.Name, NeptunCode = subject.NeptunCode };
    }

    public static Subject ToSubject(this Subject2 subject)
    {
        return new Subject { Id = subject.Id, Name = subject.Name, NeptunCode = subject.NeptunCode };
    }
}
