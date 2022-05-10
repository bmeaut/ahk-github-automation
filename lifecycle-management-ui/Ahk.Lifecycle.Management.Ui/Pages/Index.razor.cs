using System.Net.Http.Json;
using Ahk.Lifecycle.Management.Ui.Models;
using Microsoft.AspNetCore.Components;

namespace Ahk.Lifecycle.Management.Ui.Pages
{
    public partial class Index : ComponentBase
    {
        private IReadOnlyCollection<Statistics>? statistics;
        private string? message;
        private string apiKey = "";
        private string prefix = "";
        private bool noSetGrade;
        private bool noPullRequest;
        private bool noBranchCreate;

        public Index() {}

        protected override void OnInitialized()
        {
            message = "Search by filling out the inputs above";
        }

        private async Task loadStats()
        {
            using var httpClient = this.httpClientFactory.CreateClient("lifecycle-management");
            statistics = null;
            message = null;
            httpClient.DefaultRequestHeaders.Remove("x-functions-key");
            httpClient.DefaultRequestHeaders.Add("x-functions-key", apiKey);

            try
            {
                var response = await httpClient.GetAsync($"ListEventsHttpFunction/{prefix}");
                response.EnsureSuccessStatusCode();

                statistics = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<Statistics>>();
            }
            catch (HttpRequestException e)
            {
                message = e.Message;
            }
        }

        private bool filterFunc(Statistics stat)
        {
            return
                hasNoEventFilter(stat, noBranchCreate, "BranchCreateEvent") &&
                hasNoEventFilter(stat, noPullRequest, "PullRequestEvent") &&
                hasNoEventFilter(stat, noSetGrade, "SetGradeEvent");
        }

        private bool hasNoEventFilter(Statistics stat, bool isFilterOn, string type)
        {
            if (!isFilterOn)
                return true;
            return stat.Events.FirstOrDefault(o => o.Type == type) == null;
        }
    }
}
