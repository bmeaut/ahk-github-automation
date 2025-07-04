namespace Ahk.GradeManagement.Client.Network;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MockClient
{
    public Task<T> CreateAsync<T>(T entity)
    {
        return Task.FromResult(entity);
    }

    public Task<ICollection<T>> GetAllAsync<T>()
    {
        var entities = new List<T>();

        return Task.FromResult(entities as ICollection<T>);
    }

    public Task<T> GetByIdAsync<T>(Guid id) where T : new()
    {
        var result = new T();
        return Task.FromResult(result);
    }

    public Task UpdateAsync<T>(Guid id, T updatedEntity)
    {
        return Task.CompletedTask;
    }

    public Task DeleteAsync<T>(Guid id)
    {
        return Task.CompletedTask;
    }
}
