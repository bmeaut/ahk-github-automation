using System.Net.Http.Json;
using Ahk.Review.Ui.Models;
using AutoMapper;
using DTOs;

namespace Ahk.Review.Ui.Services
{
    public class SubmissionInfoService
    {
        private readonly HttpClient httpClient;

        public Mapper Mapper { get; set; }

        public SubmissionInfoService(IHttpClientFactory httpClientFactory, Mapper mapper)
        {
            this.httpClient = httpClientFactory.CreateClient("ApiClient");
            this.Mapper = mapper;
        }

        public async Task<IReadOnlyCollection<SubmissionInfo>> GetData(string repositoryPrefix, string apiKey)
        {
            httpClient.DefaultRequestHeaders.Remove("x-functions-key");
            httpClient.DefaultRequestHeaders.Add("x-functions-key", apiKey);

            var repoStat = await httpClient.GetFromJsonAsync<IReadOnlyCollection<SubmissionInfoDTO>>($"list-statuses/{repositoryPrefix}");
            var grades = await httpClient.GetFromJsonAsync<IReadOnlyCollection<FinalStudentGrade>>($"list-grades/{repositoryPrefix}");
            var events = await httpClient.GetFromJsonAsync<IReadOnlyCollection<StatusEventBaseDTO>>($"list-events/{repositoryPrefix}");

            return mergeResults(repoStat!, grades!, events!);
        }

        public async Task<Stream> DownloadGradesCsv(string repositoryPrefix, string apiKey)
        {
            httpClient.DefaultRequestHeaders.Remove("x-functions-key");
            httpClient.DefaultRequestHeaders.Add("x-functions-key", apiKey);

            using var req = new HttpRequestMessage(HttpMethod.Get, $"list-grades/{repositoryPrefix}");
            req.Headers.Remove("Accept");
            req.Headers.Add("Accept", "text/csv");
            var resp = await httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            return await resp.Content.ReadAsStreamAsync();
        }

        private static IReadOnlyCollection<SubmissionInfo> mergeResults(IReadOnlyCollection<SubmissionInfoDTO> submissionInfoDTOs, IReadOnlyCollection<FinalStudentGrade> grades, IReadOnlyCollection<StatusEventBaseDTO> events)
        {
            var gradesLookup = grades.ToDictionary(g => g.Repo);
            return submissionInfoDTOs.Select(r =>
            {
                gradesLookup.TryGetValue(r.Repository, out var g);
                return new SubmissionInfo(g.AssignmentName, r.Repository, r.Neptun, r.Branches, r.PullRequests, r.WorkflowRuns, g?.Points, events);
            }).ToList();
        }
    }
}
