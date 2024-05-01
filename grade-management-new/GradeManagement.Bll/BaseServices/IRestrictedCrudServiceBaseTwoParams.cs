namespace GradeManagement.Bll.BaseServices;

public interface IRestrictedCrudServiceBase<TRequestDto, TResponseDto> : IQueryServiceBase<TResponseDto>
{
    public Task<TResponseDto> CreateAsync(TRequestDto requestDto);

    public Task DeleteAsync(long id);
}
