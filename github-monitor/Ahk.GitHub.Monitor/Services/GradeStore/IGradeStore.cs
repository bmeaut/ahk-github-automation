using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.Services
{
    public interface IGradeStore
    {
        Task StoreGrade(string neptun, string repository, int prNumber, string prUrl, string actor, string origin, IReadOnlyCollection<double> results);
    }
}
