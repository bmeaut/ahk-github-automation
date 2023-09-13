using Ahk.Review.Ui.Models;
using AutoMapper;
using DTOs;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace Ahk.Review.Ui.Services
{
    public class GroupService
    {
        private readonly HttpClient httpClient;

        private Mapper Mapper { get; set; }

        public GroupService(IHttpClientFactory httpClientFactory, Mapper mapper)
        {
            this.httpClient = httpClientFactory.CreateClient("ApiClient");
            this.Mapper = mapper;
        }

        public async void CreateGroupAsync(Group group)
        {
            await httpClient.PostAsJsonAsync($"create-group", Mapper.Map<GroupDTO>(group));
        }

        public async Task<List<Group>> GetGroupsAsync(string subject)
        {
            var response = await httpClient.GetFromJsonAsync<OkObjectResult>($"list-groups/{subject}");
            var groupDTOs = JsonConvert.DeserializeObject<List<GroupDTO>>(response.Value.ToString());

            return groupDTOs.Select(gDTO =>
            {
                return new Group(gDTO);
            }).ToList();
        }

        public async Task<Group> GetGroupAsync(string subject, string groupId)
        {
            var groups = await GetGroupsAsync(subject);
            var group = groups.Where(g => g.Id.ToString() == groupId).FirstOrDefault();

            return group;
        }

        public async Task UpdateGroupAsync(string subject, Group group)
        {
            await httpClient.PostAsJsonAsync<GroupDTO>($"edit-group/{subject}", Mapper.Map<GroupDTO>(group));
        }

        public async Task DeleteGroupAsync(string groupId)
        {
            await httpClient.DeleteAsync($"delete-group/{groupId}");
        }
    }
}
