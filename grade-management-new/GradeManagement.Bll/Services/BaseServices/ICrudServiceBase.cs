namespace GradeManagement.Bll.Services.BaseServices;

public interface ICrudServiceBase<TDto> : IRestrictedCrudServiceBase<TDto>
{

    public Task<TDto> UpdateAsync(long id, TDto requestDto);

}
