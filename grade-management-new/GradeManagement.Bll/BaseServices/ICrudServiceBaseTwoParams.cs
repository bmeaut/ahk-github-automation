namespace GradeManagement.Bll.BaseServices;

public interface ICrudServiceBase<TRequestDto, TResponseDto> : IRestrictedServiceBase<TRequestDto, TResponseDto>
{

    public Task<TResponseDto> UpdateAsync(long id, TRequestDto requestDto);

}
