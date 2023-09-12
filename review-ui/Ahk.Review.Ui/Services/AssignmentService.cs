using Ahk.Review.Ui.Models;
using AutoMapper;
using DTOs;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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

        public async void PostDataAsync(Assignment assignment)
        {
            await httpClient.PostAsJsonAsync($"create-assignment", Mapper.Map<AssignmentDTO>(assignment));
        }

        public async Task<List<Assignment>> GetAssignmentsAsync(string subject)
        {
            var response = await httpClient.GetFromJsonAsync<OkObjectResult>($"list-assignments/{subject}");
            var assignmentDTOs = JsonConvert.DeserializeObject<List<AssignmentDTO>>(response.Value.ToString());

            return assignmentDTOs.Select(aDTO =>
            {
                return new Assignment(aDTO);
            }).ToList();
        }
    }
}
