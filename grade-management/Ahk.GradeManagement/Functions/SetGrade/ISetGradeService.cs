using System.Threading.Tasks;

namespace Ahk.GradeManagement.SetGrade
{
    public interface ISetGradeService
    {
        Task SetGrade(SetGradeEvent data);
    }
}