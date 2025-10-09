using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.Services.GradeStore;

public interface IGradeStore
{
    public Task StoreGradeAsync(string repositoryUrl, string prUrl, string actor, Dictionary<int, double> results);
    public Task ConfirmAutoGradeAsync(string repositoryUrl, string prUrl, string actor);
}
