using System;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Memory;

namespace Ahk.GradeManagement.Services
{
    internal class TokenManagementService : ITokenManagementService
    {
        private readonly IMemoryCache cache;
        public AhkDbContext Context { get; set; }

        public TokenManagementService(AhkDbContext context,IMemoryCache cache)
        {
            this.Context = context;
            this.cache = cache;
        }

        public async Task SetSecretAsync(string token, string secret, string description)
        {
            Context.WebhookTokens.Add(new Data.Entities.WebhookToken() { Id = token, Secret = secret, Description = description });
            await Context.SaveChangesAsync();
            cache.Remove(getCacheKeyForToken(token));
        }

        public Task<string> GetSecretForTokenAsync(string token)
            => cache.GetOrCreateAsync(
                key: getCacheKeyForToken(token),
                factory: async cacheEntry =>
                {
                    var value = await getSecretForTokenFromDbAsync(token);
                    cacheEntry.SetValue(value);
                    cacheEntry.SetAbsoluteExpiration(TimeSpan.FromHours(1));
                    return value;
                });

        private static string getCacheKeyForToken(string token) => $"secrettotoken{token}";

        private async Task<string> getSecretForTokenFromDbAsync(string token)
        {
            var val = await Context.WebhookTokens.FindAsync(token);
            return val?.Secret;
        }
    }
}
