using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.Services.GradeStore
{
    internal class GradeStoreNoop : IGradeStore
    {
        public Task StoreGrade(string repositoryUrl, string prUrl, string actor, Dictionary<int, double> results) =>
            Task.CompletedTask;

        public Task ConfirmAutoGrade(string repositoryUrl, string prUrl, string actor) => Task.CompletedTask;
    }
}
