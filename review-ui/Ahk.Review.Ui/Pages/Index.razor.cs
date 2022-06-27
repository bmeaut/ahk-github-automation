using Ahk.Review.Ui.Models;
using Ahk.Review.Ui.Services;
using Microsoft.AspNetCore.Components;

namespace Ahk.Review.Ui.Pages
{
    public partial class Index : ComponentBase
    {
        [Inject]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private DataService DataService { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        // displayed info
        private bool loaded = false;
        private IReadOnlyCollection<SubmissionInfo> repoList = new SubmissionInfo[0];
        private string? message;
        private bool fetchingData;

        // inputs
        private string apiKey = "";
        private string repoPrefix = "";

        // filters
        private bool filterNoBranch;
        private bool filterNoPr;
        private bool filterNoCiWorkflow;
        private bool filterNoGrade;

        protected override void OnInitialized()
        {
            loaded = true;
        }

        private async Task loadStats()
        {
            this.fetchingData = true;
            try
            {
                this.repoList = await DataService.GetData(repoPrefix, apiKey);
                this.message = null;
            }
            catch (Exception ex)
            {
                this.repoList = new SubmissionInfo[0];
                this.message = ex.ToString();
            }
            finally
            {
                this.fetchingData = false;
            }
        }

        private bool filterFunc(SubmissionInfo value)
            => (!filterNoBranch || value.Branches.Count == 0)
                && (!filterNoPr || value.PullRequests.Count == 0)
                && (!filterNoCiWorkflow || value.WorkflowRuns.Count == 0)
                && (!filterNoGrade || string.IsNullOrEmpty(value.Grade));
    }
}
