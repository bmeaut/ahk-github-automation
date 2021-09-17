using System.Threading.Tasks;

namespace Ahk.GradeManagement.Services
{
    public interface ITokenManagementService
    {
        Task<string> GetSecretForToken(string token);
        Task SetSecret(string token, string secret, string description);
    }
}