using Ahk.Review.Ui.Models;
using Ahk.Review.Ui.Services;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;

namespace Ahk.Review.Ui.Components
{
    public partial class Header : ComponentBase
    {
        [Inject]
        private SubjectService Service { get; set; }
        [Inject]
        private NavigationManager NavigationManager { get; set; }

        private string subjectCode = string.Empty;
        private List<Subject> subjects = new List<Subject>();


        protected override async void OnInitialized()
        {
            var results = await Service.GetSubjects();
            subjects = results.ToList();
        }

        private void SetTenant()
        {
            Service.SetCurrentTenant(subjectCode, subjects.Where(s => s.SubjectCode == subjectCode).FirstOrDefault());
            Console.WriteLine(Service.TenantCode);
            Console.WriteLine(JsonConvert.SerializeObject(Service.CurrentTenant));
        }
    }
}
