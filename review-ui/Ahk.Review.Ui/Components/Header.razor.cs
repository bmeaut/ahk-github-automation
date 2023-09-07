using Ahk.Review.Ui.Models;
using Ahk.Review.Ui.Services;
using Microsoft.AspNetCore.Components;

namespace Ahk.Review.Ui.Components
{
    public partial class Header : ComponentBase
    {
        [Inject]
        private SubjectService service { get; set; }
        private string subjectCode = string.Empty;
        private List<Subject> subjects = new List<Subject>();

        protected override async void OnInitialized()
        {
            subjects = service.GetSubjects().Result.ToList();
        }

        private void SetTenant()
        {
            service.Tenant = subjectCode;
        }
    }
}
