using Ahk.GradeManagement.Data;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.Services
{
    internal class TokenManagementService : ITokenManagementService
    {
        private readonly AhkDb db;
        private readonly IMemoryCache cache;

        public TokenManagementService(AhkDb db, IMemoryCache cache)
        {
            this.db = db;
            this.cache = cache;
        }

        public async Task SetSecret(string token, string secret, string description)
        {
            await this.db.EnsureCreated();

            var existing = await this.db.WebhookTokens.FindAsync(token);
            if (existing != null)
            {
                existing.Secret = secret;
                existing.Description = description;
            }
            else
            {
                this.db.WebhookTokens.Add(new Data.Entities.WebhookToken() { Id = token, Secret = secret, Description = description });
            }

            await this.db.SaveChangesAsync();
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
            await this.db.EnsureCreated();
            var val = await this.db.WebhookTokens.FindAsync(token);
            return val?.Secret;
        }
    }
}
