using System.Threading.Tasks;
using Ahk.GradeManagement.ResultProcessing.Dto;

namespace Ahk.GradeManagement.ResultProcessing
{
    public interface IResultProcessor
    {
        Task<string> GetSecretForToken(string token);
        Task ProcessResult(AhkProcessResult value, System.DateTime dateTime);
    }
}
