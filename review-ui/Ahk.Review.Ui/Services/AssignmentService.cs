using Ahk.Review.Ui.Models;
using AutoMapper;
using DTOs;
using System.Net.Http.Json;

namespace Ahk.Review.Ui.Services
{
    public class AssignmentService
    {
        private readonly HttpClient httpClient;

        public Mapper Mapper { get; set; }

        public AssignmentService(IHttpClientFactory httpClientFactory, Mapper mapper)
        {
            this.httpClient = httpClientFactory.CreateClient("ApiClient");
            this.Mapper = mapper;
        }

        public async void PostData(Assignment assignment, string apiKey)
        {
            httpClient.DefaultRequestHeaders.Remove("x-functions-key");
            httpClient.DefaultRequestHeaders.Add("x-functions-key", apiKey);

            await httpClient.PostAsJsonAsync($"create-assignment", Mapper.Map<AssignmentDTO>(assignment));
        }

        public async Task<IReadOnlyCollection<Assignment>> GetAssignments(string subject, string apiKey)
        {
            httpClient.DefaultRequestHeaders.Remove("x-functions-key");
            httpClient.DefaultRequestHeaders.Add("x-functions-key", apiKey);

            var assignments = await httpClient.GetFromJsonAsync<IReadOnlyCollection<AssignmentDTO>>($"list-assignments/{subject}");

            return assignments;
        }
    }
}
