using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;

namespace Ahk.GradeManagement.Data
{
    public interface IWebhookTokenRepository
    {
        Task<WebhookToken> FindToken(string token);
        Task UpsertToken(WebhookToken value);
    }
}