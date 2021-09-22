using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;
using Microsoft.Azure.Cosmos;

namespace Ahk.GradeManagement.Data.Internal
{
    internal class ResultsRepository : RepositoryBase<StudentResult>, IResultsRepository
    {
        internal const string ContainerName = "grades";

        public ResultsRepository(CosmosClient client)
            : base(client, ContainerName, DefaultPartitionKey)
        {
        }

        public Task AddResult(StudentResult value) => base.Insert(value, value.Id);
    }
}
