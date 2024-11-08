using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.Services.GradeStore;

public interface IGradeStore
{
    Task StoreGrade(string repositoryUrl, string prUrl, string actor, Dictionary<int, double> results);
    Task ConfirmAutoGrade(string repositoryUrl, string prUrl, string actor);
}
