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
        [Inject]
        private NavigationManager NavigationManager { get; set; }

        private string courseCode;
        private string subjectName;
        private string semester;
        private string githubOrg;

        private List<Group> groups;
        private List<Assignment> assignments;

        private bool firstRender = true;

        protected override async void OnInitialized()
        {
            if (firstRender)
            {
                courseCode = SubjectService.TenantCode;
                subjectName = SubjectService.CurrentTenant.Name;
                semester = SubjectService.CurrentTenant.Semester;
                githubOrg = SubjectService.CurrentTenant.GithubOrg;

                groups = await GroupService.GetGroupsAsync(SubjectService.TenantCode);
                assignments = await AssignmentService.GetAssignmentsAsync(SubjectService.TenantCode);

                firstRender = false;

                StateHasChanged();
            }

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

        private void EditGroup(int groupId)
        {
            NavigationManager.NavigateTo($"/edit-group/{groupId}");
        }

        private async Task DeleteGroup(int groupId)
        {
            await GroupService.DeleteGroupAsync(groupId.ToString());
        }

        private void ShowGroupDetails(int groupId)
        {
            NavigationManager.NavigateTo($"/group-details/{groupId}");
        }

        private void EditAssignment(int assignmentId)
        {
            NavigationManager.NavigateTo($"/edit-assignment/{assignmentId}");
        }

        private async Task DeleteAssignment(int assignmentId)
        {
            await AssignmentService.DeleteAssignmentAsync(assignmentId.ToString());
        }

        private void ShowAssignmentDetails(int assignmentId)
        {
            NavigationManager.NavigateTo($"/assignment-details/{assignmentId}");
        }
    }
}
