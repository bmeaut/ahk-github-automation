using Ahk.GradeManagement.ResultProcessing.Dto;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.ResultProcessing
{
    public interface IResultProcessor
    {
        Task<string> GetSecretForToken(string token);
        Task ProcessResult(AhkProcessResult value);
    }
}
