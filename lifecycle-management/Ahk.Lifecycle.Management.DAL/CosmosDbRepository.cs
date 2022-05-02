using Ahk.Lifecycle.Management.DAL.Dto;
using Ahk.Lifecycle.Management.DAL.Entities;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Ahk.Lifecycle.Management.DAL
{
    public class CosmosDbRepository : IRepository
    {
        private const string databaseName = "ahk";
        private const string eventsContainerName = "events";
        private const string eventsContainerPartitionKey = "/id";

        private readonly Database database;
        private readonly Container eventsContainer;
        private volatile bool created;

        public CosmosDbRepository(CosmosClient client)
        {
            this.database = client.GetDatabase(databaseName);
            this.eventsContainer = database.GetContainer(eventsContainerName);
        }

        private async Task ensureCreated()
        {
            if (created)
                return;

            await this.database.Client.CreateDatabaseIfNotExistsAsync(databaseName);
            await this.database.CreateContainerIfNotExistsAsync(new ContainerProperties() { Id = eventsContainerName, PartitionKeyPath = eventsContainerPartitionKey });

            created = true;
        }

        public async Task<IReadOnlyCollection<Statistics>> GetRepositories(string prefix = "")
        {
            await this.ensureCreated();

            var data = new List<LifecycleEvent>();

            using var iter = eventsContainer
                .GetItemLinqQueryable<LifecycleEvent>(allowSynchronousQueryExecution: true)
                .Where(o => o.Repository.StartsWith(prefix))
                .ToFeedIterator();

            while (iter.HasMoreResults)
                data.AddRange(await iter.ReadNextAsync());

            var results = data
                    .GroupBy(o => o.Repository)
                    .Select(o => new Statistics(
                        repository: o.Key,
                        count: o.Count(),
                        events: o.ToList()))
                    .ToList();

            return results;
        }

        public async Task Insert<T>(T data) where T : LifecycleEvent
        {
            await this.ensureCreated();
            await eventsContainer.CreateItemAsync<T>(data, new PartitionKey(data.Id));
        }
    }
}
