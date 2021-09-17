using System;

namespace Ahk.GradeManagement
{
    public interface IDateTimeProvider
    {
        DateTime GetUtcNow();
    }
}
