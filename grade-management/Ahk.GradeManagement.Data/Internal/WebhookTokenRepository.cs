using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;

namespace Ahk.GradeManagement.Data.Internal
{
    internal class WebhookTokenRepository : IWebhookTokenRepository
    {
        public AhkDbContext Context { get; set; }

        public WebhookTokenRepository()
        {
        }

        public Task<WebhookToken> FindToken(string token) => throw new System.NotImplementedException();
        public Task UpsertToken(WebhookToken value) => throw new System.NotImplementedException();
    }
}
