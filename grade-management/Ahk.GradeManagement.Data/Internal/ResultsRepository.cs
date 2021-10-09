using Ahk.GradeManagement.Data.Entities;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        public Task<IReadOnlyCollection<StudentResult>> ListConfirmedWithRepositoryPrefix(string repoPrefix) => base.List(s => s.Confirmed && s.GitHubRepoName.StartsWith(repoPrefix));
        public Task<StudentResult> GetLastResultOf(string neptun, string gitHubRepoName, int gitHubPrNumber)
            => base.GetOneWithOrderByDescending(
                predicate: s => s.Neptun == neptun.ToUpperInvariant() && s.GitHubRepoName == gitHubRepoName.ToLowerInvariant() && s.GitHubPrNumber == gitHubPrNumber,
                orderBy: s => s.Date);

    }
}
