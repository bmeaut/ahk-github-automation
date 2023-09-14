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

        public async Task<Assignment> GetAssignmentAsync(string subject, string assignmentId)
        {
            var assignments = await GetAssignmentsAsync(subject);
            var assignment = assignments.Where(a => a.Id.ToString() == assignmentId).FirstOrDefault();

            return assignment;
        }
        
        public async Task<List<Exercise>> GetExercisesAsync(string subject, string assignmentId)
        {
            var response = await httpClient.GetFromJsonAsync<OkObjectResult>($"list-exercises/{subject}/{assignmentId}");
            var exerciseDTOs = JsonConvert.DeserializeObject<List<ExerciseDTO>>(response.Value.ToString());

            return exerciseDTOs.Select(eDTO =>
            {
                return new Exercise(eDTO);
            }).ToList();
        }

        public async Task DeleteAssignmentAsync(string assignmentId)
        {

        }
    }
}
