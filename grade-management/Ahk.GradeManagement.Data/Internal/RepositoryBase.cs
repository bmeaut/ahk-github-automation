using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Ahk.GradeManagement.Data.Internal
{
    internal abstract class RepositoryBase<T>
        where T : class
    {
        internal const string DatabaseName = "ahk";
        internal const string DefaultPartitionKey = "/id";

        private static ItemRequestOptions roNoReadBack = new ItemRequestOptions() { EnableContentResponseOnWrite = false };

        private readonly Database db;
        private readonly Container container;
        private readonly string containerName;
        private readonly string partitionKeyPath;

        private volatile bool initialized = false;

        public RepositoryBase(CosmosClient client, string containerName, string partitionKeyPath)
        {
            this.db = client.GetDatabase(DatabaseName);
            this.container = db.GetContainer(containerName);
            this.containerName = containerName;
            this.partitionKeyPath = partitionKeyPath;
        }

        protected async Task Insert(T value, string partitionKey)
        {
            await this.ensureCreated();
            await container.CreateItemAsync(value, new PartitionKey(partitionKey), roNoReadBack);
        }

        protected async Task Upsert(T value, string partitionKey)
        {
            await this.ensureCreated();
            await container.UpsertItemAsync(value, new PartitionKey(partitionKey), roNoReadBack);
        }

        public async Task<T> FindById(string id, string partitionKey)
        {
            await this.ensureCreated();
            try
            {
                var i = await container.ReadItemAsync<T>(id, new PartitionKey(partitionKey));
                return i.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IReadOnlyCollection<T>> List(System.Linq.Expressions.Expression<System.Func<T, bool>> predicate)
        {
            await this.ensureCreated();

            var results = new List<T>();
            using var iter = container.GetItemLinqQueryable<T>().Where(predicate).ToFeedIterator();
            while (iter.HasMoreResults)
                results.AddRange(await iter.ReadNextAsync());

            return results;
        }

        private async ValueTask ensureCreated()
        {
            if (initialized)
                return;

            await this.db.Client.CreateDatabaseIfNotExistsAsync(DatabaseName);
            await db.CreateContainerIfNotExistsAsync(new ContainerProperties() { Id = this.containerName, PartitionKeyPath = partitionKeyPath });
            this.initialized = true;
        }
    }
}
