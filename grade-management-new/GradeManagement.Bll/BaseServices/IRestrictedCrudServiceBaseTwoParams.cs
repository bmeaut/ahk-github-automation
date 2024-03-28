namespace GradeManagement.Bll.BaseServices;

public interface IRestrictedCrudServiceBase<TRequestDto, TResponseDto>
{
    public Task<IEnumerable<TResponseDto>> GetAllAsync();

    public Task<TResponseDto> GetByIdAsync(long id);

    public Task<TResponseDto> CreateAsync(TRequestDto requestDto);

    public Task DeleteAsync(long id);
}
