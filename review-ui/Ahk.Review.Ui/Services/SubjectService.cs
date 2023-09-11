using Ahk.Review.Ui.Models;
using AutoMapper;
using DTOs;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace Ahk.Review.Ui.Services
{
    public class SubjectService
    {
        private readonly HttpClient httpClient;
        public string Tenant { get; set; }

        public Mapper Mapper { get; set; }

        public SubjectService(IHttpClientFactory httpClientFactory, Mapper mapper)
        {
            this.httpClient = httpClientFactory.CreateClient("ApiClient");
            this.Mapper = mapper;
        }

        public async Task<IReadOnlyCollection<Subject>> GetSubjects()
        {
            var results = await httpClient.GetFromJsonAsync<OkObjectResult>($"list-subjects");

            var subjectDTOs = JsonConvert.DeserializeObject<List<SubjectDTO>>(results.Value.ToString());

            return subjectDTOs.Select(sDTO =>
            {
                return new Subject(sDTO);
            }).ToList();
        }
    }
}
