namespace GradeManagement.Bll.BaseServices;

public interface IQueryServiceBase<TDto>
{
    public Task<IEnumerable<TDto>> GetAllAsync();

    public Task<TDto> GetByIdAsync(long id);
}
