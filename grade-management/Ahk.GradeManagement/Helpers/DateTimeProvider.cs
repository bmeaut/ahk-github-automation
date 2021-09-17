using System;

namespace Ahk.GradeManagement
{
    internal class DateTimeProvider : IDateTimeProvider
    {
        public DateTime GetUtcNow() => DateTime.UtcNow;
    }
}
