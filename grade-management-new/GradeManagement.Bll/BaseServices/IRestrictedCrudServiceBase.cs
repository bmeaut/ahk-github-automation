namespace GradeManagement.Bll.BaseServices;

public interface IRestrictedCrudServiceBase<TDto> : IQueryServiceBase<TDto>
{
    public Task<TDto> CreateAsync(TDto requestDto);

    public Task DeleteAsync(long id);
}
