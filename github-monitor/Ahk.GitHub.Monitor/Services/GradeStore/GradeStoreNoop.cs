using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.Services
{
    internal class GradeStoreNoop : IGradeStore
    {
        public Task StoreGrade(string neptun, string repository, int prNumber, string prUrl, string actor, string origin, IReadOnlyCollection<double> results) => Task.CompletedTask;
        public Task ConfirmAutoGrade(string neptun, string repository, int prNumber, string prUrl, string actor, string origin) => Task.CompletedTask;
    }
}
