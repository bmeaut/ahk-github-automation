using Ahk.Lifecycle.Management.DAL.Dto;
using Ahk.Lifecycle.Management.DAL.Entities;

namespace Ahk.Lifecycle.Management.DAL
{
    public interface IRepository
    {
        Task Insert<T>(T data) where T : LifecycleEvent;
        Task<IReadOnlyCollection<Statistics>> GetRepositories(string prefix = "");
    }
}
