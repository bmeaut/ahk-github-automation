using Ahk.Review.Ui.Models;
using AutoMapper;
using DTOs;
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
            var subjectDTOs = await httpClient.GetFromJsonAsync<IReadOnlyCollection<SubjectDTO>>($"list-subjects");

            return subjectDTOs.Select(sDTO =>
            {
                return new Subject(sDTO);
            }).ToList();
        }
    }
}
