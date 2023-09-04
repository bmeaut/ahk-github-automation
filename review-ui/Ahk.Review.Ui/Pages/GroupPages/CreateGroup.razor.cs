using Ahk.Review.Ui.Services;
using Microsoft.AspNetCore.Components;

namespace Ahk.Review.Ui.Pages.GroupPages
{
    public partial class CreateGroup : ComponentBase
    {
        [Inject]
        private GroupService groupService { get; set; }
        private bool loaded = false;

        protected override void OnInitialized()
        {
            loaded = true;
        }
    }
}
