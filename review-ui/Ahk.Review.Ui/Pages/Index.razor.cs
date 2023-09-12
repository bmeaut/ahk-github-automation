using Ahk.Review.Ui.Models;
using Ahk.Review.Ui.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using DTOs;

namespace Ahk.Review.Ui.Pages
{
    public partial class Index : ComponentBase
    {
        // displayed info
        private bool loaded = false;
        private IReadOnlyCollection<SubmissionInfo> repoList = Array.Empty<SubmissionInfo>();
        private string? message;
        private bool fetchingData;

        // inputs
        private string apiKey = string.Empty;
        private string repoPrefix = string.Empty;

        // filters
        private bool filterNoBranch;
        private bool filterNoPr;
        private bool filterNoCiWorkflow;
        private bool filterNoGrade;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        [Inject]
        private SubmissionInfoService DataService { get; set; }
        [Inject]
        private SubjectService SubjectService { get; set; }
        [Inject]
        private IJSRuntime JS { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        protected override void OnInitialized()
        {
            loaded = true;
        }

        private async Task loadData()
        {
            if (string.IsNullOrEmpty(SubjectService.TenantCode) || SubjectService.CurrentTenant == null)
            {
                await JS.InvokeAsync<object>("alert", "Subject not selected!");
            }
            else
            {
                this.fetchingData = true;
                try
                {
                    this.repoList = await DataService.GetData(repoPrefix, apiKey);
                    this.message = null;
                }
                catch (Exception ex)
                {
                    this.repoList = Array.Empty<SubmissionInfo>();
                    this.message = ex.ToString();
                }
                finally
                {
                    this.fetchingData = false;
                }
            }
        }

        private async Task downloadGradeCsv()
        {
            try
            {
                var fileName = $"{repoPrefix}-{DateTime.Now.ToString("yyyy-MM-dd-HH-mm", System.Globalization.CultureInfo.InvariantCulture)}".Replace("/", "-", StringComparison.OrdinalIgnoreCase);
                var csvBytes = await DataService.DownloadGradesCsv(repoPrefix, apiKey);
                using var streamRef = new DotNetStreamReference(csvBytes);
                await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
            }
            catch (Exception ex)
            {
                this.message = ex.ToString();
            }
        }

        private bool filterFunc(SubmissionInfo value)
            => (!filterNoBranch || value.Branches.Count == 0)
                && (!filterNoPr || value.PullRequests.Count == 0)
                && (!filterNoCiWorkflow || value.WorkflowRuns.Count == 0)
                && (!filterNoGrade || string.IsNullOrEmpty(value.Grade));
    }
}
