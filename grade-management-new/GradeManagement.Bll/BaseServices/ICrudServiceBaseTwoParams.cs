namespace GradeManagement.Bll.BaseServices;

public interface ICrudServiceBase<TRequestDto, TResponseDto> : IRestrictedCrudServiceBase<TRequestDto, TResponseDto>
{

    public Task<TResponseDto> UpdateAsync(long id, TRequestDto requestDto);

}
