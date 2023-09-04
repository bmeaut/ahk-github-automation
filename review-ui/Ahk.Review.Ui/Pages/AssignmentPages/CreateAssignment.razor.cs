using Ahk.Review.Ui.Services;
using Microsoft.AspNetCore.Components;

namespace Ahk.Review.Ui.Pages.AssignmentPages
{
    public partial class CreateAssignment : ComponentBase
    {
        [Inject]
        private AssignmentService service { get; set; }
        private bool loaded = false;

        protected override void OnInitialized()
        {
            loaded = true;
        }
    }
}
