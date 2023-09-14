using Ahk.Review.Ui.Models;
using Ahk.Review.Ui.Services;
using Microsoft.AspNetCore.Components;

namespace Ahk.Review.Ui.Pages.AssignmentPages
{
    public partial class EditAssignment : ComponentBase
    {
        [Parameter]
        public string Subject { get; set; }
        [Parameter]
        public string AssignmentId { get; set; }

        private Assignment assignment;
        private List<Exercise> exercises;

        [Inject]
        private AssignmentService AssignmentService { get; set; }

        protected override async void OnInitialized()
        {
            assignment = await AssignmentService.GetAssignmentAsync(Subject, AssignmentId);
            exercises = await AssignmentService.GetExercisesAsync(Subject, AssignmentId);
        }
    }
}
