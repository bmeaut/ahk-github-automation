using Ahk.Review.Ui.Services;
using Microsoft.AspNetCore.Components;

namespace Ahk.Review.Ui.Pages.SubjectPages
{
    public partial class CreateSubject : ComponentBase
    {
        [Inject]
        private SubjectService SubjectService { get; set; }
    }
}
