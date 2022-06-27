using System.Net.Http.Json;
using Ahk.Review.Ui.Models;

namespace Ahk.Review.Ui.Services
{
    public class DataService
    {
        private readonly HttpClient httpClient;

        public DataService(IHttpClientFactory httpClientFactory)
        {
            this.httpClient = httpClientFactory.CreateClient("ApiClient");
        }

        public async Task<IReadOnlyCollection<SubmissionInfo>> GetData(string repositoryPrefix, string apiKey)
        {
            httpClient.DefaultRequestHeaders.Remove("x-functions-key");
            httpClient.DefaultRequestHeaders.Add("x-functions-key", apiKey);

            var repoStat = await httpClient.GetFromJsonAsync<IReadOnlyCollection<RepositoryStatus>>($"list-statuses/{repositoryPrefix}");
            var grades = await httpClient.GetFromJsonAsync<IReadOnlyCollection<FinalStudentGrade>>($"list-grades/{repositoryPrefix}");

            return mergeResults(repoStat!, grades!);
        }

        private static IReadOnlyCollection<SubmissionInfo> mergeResults(IReadOnlyCollection<RepositoryStatus> repoStat, IReadOnlyCollection<FinalStudentGrade> grades)
        {
            var gradesLookup = grades.ToDictionary(g => g.Repo);
            return repoStat.Select(r =>
            {
                gradesLookup.TryGetValue(r.Repository, out var g);
                return new SubmissionInfo(r, g?.Points);
            }).ToList();
        }
    }
}
