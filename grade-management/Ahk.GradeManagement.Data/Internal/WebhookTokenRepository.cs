using System.Linq;
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

        public async Task<WebhookToken> FindToken(string token)
        {
            return await Context.WebhookTokens.FindAsync(token);
        }
        public async Task UpsertToken(WebhookToken value)
        {
            Context.WebhookTokens.Add(value);
            await Context.SaveChangesAsync();
        }
    }
}
