using System.Threading.Tasks;
using Ahk.GradeManagement.ResultProcessing.Dto;

namespace Ahk.GradeManagement.ResultProcessing
{
    public interface IResultProcessor
    {
        Task<string> GetSecretForTokenAsync(string token);
        Task ProcessResultAsync(AhkProcessResult value, System.DateTime dateTime);
    }
}
