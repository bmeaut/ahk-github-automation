using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;
using Microsoft.Azure.Cosmos;

namespace Ahk.GradeManagement.Data.Internal
{
    internal class WebhookTokenRepository : RepositoryBase<WebhookToken>, IWebhookTokenRepository
    {
        internal const string ContainerName = "webhooktokens";

        public WebhookTokenRepository(CosmosClient client)
            : base(client, ContainerName, DefaultPartitionKey)
        {
        }

        public Task UpsertToken(WebhookToken value) => base.Upsert(value, value.Id);
        public Task<WebhookToken> FindToken(string token) => base.FindById(token, token);
    }
}
