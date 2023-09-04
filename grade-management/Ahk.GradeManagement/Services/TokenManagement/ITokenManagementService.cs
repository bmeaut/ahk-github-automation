using System.Threading.Tasks;

namespace Ahk.GradeManagement.Services
{
    public interface ITokenManagementService
    {
        Task<string> GetSecretForTokenAsync(string token);
        Task SetSecretAsync(string token, string secret, string description);
    }
}
