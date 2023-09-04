using System.Threading.Tasks;
using Ahk.GradeManagement.SetGrade;

namespace Ahk.GradeManagement.Services.SetGradeService
{
    public interface ISetGradeService
    {
        Task SetGradeAsync(SetGradeEvent data);
        Task ConfirmAutoGradeAsync(ConfirmAutoGradeEvent data);
    }
}
