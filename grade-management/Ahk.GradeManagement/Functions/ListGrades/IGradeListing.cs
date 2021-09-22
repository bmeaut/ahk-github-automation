using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.ListGrades
{
    public interface IGradeListing
    {
        Task<IReadOnlyCollection<FinalStudentGrade>> List(string repoPrefix);
    }
}