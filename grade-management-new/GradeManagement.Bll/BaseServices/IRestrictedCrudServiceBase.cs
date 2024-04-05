namespace GradeManagement.Bll.BaseServices;

public interface IRestrictedCrudServiceBase<TDto>
{
    public Task<IEnumerable<TDto>> GetAllAsync();

    public Task<TDto> GetByIdAsync(long id);

    public Task<TDto> CreateAsync(TDto requestDto);

    public Task DeleteAsync(long id);
}
