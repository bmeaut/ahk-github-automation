using System;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data;
using Microsoft.Extensions.Caching.Memory;

namespace Ahk.GradeManagement.Services
{
    internal class TokenManagementService : ITokenManagementService
    {
        private readonly IWebhookTokenRepository repo;
        private readonly IMemoryCache cache;

        public TokenManagementService(IWebhookTokenRepository repo, IMemoryCache cache)
        {
            this.repo = repo;
            this.cache = cache;
        }

        public async Task SetSecret(string token, string secret, string description)
        {
            await this.repo.UpsertToken(new Data.Entities.WebhookToken() { Id = token, Secret = secret, Description = description });
            cache.Remove(getCacheKeyForToken(token));
        }

        public Task<string> GetSecretForToken(string token)
            => cache.GetOrCreateAsync(
                key: getCacheKeyForToken(token),
                factory: async cacheEntry =>
                {
                    var value = await getSecretForTokenFromDb(token);
                    cacheEntry.SetValue(value);
                    cacheEntry.SetAbsoluteExpiration(TimeSpan.FromHours(1));
                    return value;
                });

        private static string getCacheKeyForToken(string token) => $"secrettotoken{token}";

        private async Task<string> getSecretForTokenFromDb(string token)
        {
            var val = await this.repo.FindToken(token);
            return val?.Secret;
        }
    }
}
