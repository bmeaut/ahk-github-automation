using Microsoft.AspNetCore.Components;

namespace Ahk.Review.Ui.Pages.GroupPages
{
    public partial class GroupDetails : ComponentBase
    {
        [Parameter]
        public string GroupId { get; set; }
    }
}
