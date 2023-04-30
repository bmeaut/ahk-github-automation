using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;

namespace Ahk.GradeManagement.Data
{
    public interface IWebhookTokenRepository
    {
        public AhkDbContext Context { get; set; }
        Task<WebhookToken> FindToken(string token);
        Task UpsertToken(WebhookToken value);
    }
}
