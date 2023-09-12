using Ahk.Review.Ui.Models;
using Ahk.Review.Ui.Services;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;

namespace Ahk.Review.Ui.Pages.SubjectPages
{
    public partial class SubjectDetails : ComponentBase, IDisposable
    {
        [Inject]
        private SubjectService SubjectService { get; set; }
        [Inject]
        private GroupService GroupService { get; set; }
        [Inject]
        private AssignmentService AssignmentService { get; set; }

        private string courseCode;
        private string subjectName;
        private string semester;
        private string githubOrg;

        private List<Group> groups;
        private List<Assignment> assignments;

        protected override async void OnInitialized()
        {
            courseCode = SubjectService.TenantCode;
            subjectName = SubjectService.CurrentTenant.Name;
            semester = SubjectService.CurrentTenant.Semester;
            githubOrg = SubjectService.CurrentTenant.GithubOrg;

            groups = await GroupService.GetGroupsAsync(SubjectService.TenantCode);
            assignments = await AssignmentService.GetAssignmentsAsync(SubjectService.TenantCode);

            SubjectService.OnChange += SubjectChanged;
        }

        public void Dispose()
        {
            SubjectService.OnChange -= SubjectChanged;
        }

        private async void SubjectChanged()
        {
            courseCode = SubjectService.TenantCode;
            subjectName = SubjectService.CurrentTenant.Name;
            semester = SubjectService.CurrentTenant.Semester;
            githubOrg = SubjectService.CurrentTenant.GithubOrg;

            groups = await GroupService.GetGroupsAsync(SubjectService.TenantCode);
            assignments = await AssignmentService.GetAssignmentsAsync(SubjectService.TenantCode);

            StateHasChanged();
        }
    }
}
