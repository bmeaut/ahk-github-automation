using Ahk.Review.Ui.Models;
using AutoMapper;
using DTOs;
using System.Net.Http.Json;

namespace Ahk.Review.Ui.Services
{
    public class GroupService
    {
        private readonly HttpClient httpClient;

        public Mapper Mapper { get; set; }

        public GroupService(IHttpClientFactory httpClientFactory, Mapper mapper)
        {
            this.httpClient = httpClientFactory.CreateClient("ApiClient");
            this.Mapper = mapper;
        }

        public async void PostData(Group group, string apiKey)
        {
            httpClient.DefaultRequestHeaders.Remove("x-functions-key");
            httpClient.DefaultRequestHeaders.Add("x-functions-key", apiKey);

            await httpClient.PostAsJsonAsync($"create-group", Mapper.Map<GroupDTO>(group));
        }
    }
}
