using Ahk.Review.Ui.Models;
using Ahk.Review.Ui.Services;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;

namespace Ahk.Review.Ui.Pages.GroupPages
{
    public partial class EditGroup : ComponentBase
    {
        [Parameter]
        public string GroupId { get; set; }

        [Inject]
        private GroupService GroupService { get; set; }
        [Inject]
        private SubjectService SubjectService { get; set; }
        [Inject]
        private NavigationManager NavigationManager { get; set; }

        private Group group;

        private string name;
        private string room;
        private string time;

        protected override async void OnInitialized()
        {
            group = await GroupService.GetGroupAsync(SubjectService.TenantCode, GroupId);

            name = group.Name;
            room = group.Room;
            time = group.Time;

            StateHasChanged();
        }

        private async void SubmitAsync()
        {
            Group update = group;
            update.Name = name;
            update.Room = room;
            update.Time = time;

            await GroupService.UpdateGroupAsync(SubjectService.TenantCode, update);
        }
    }
}
