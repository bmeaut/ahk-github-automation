namespace GradeManagement.Bll.BaseServices;

public interface ICrudServiceBase<DtoClass> : IRestrictedServiceBase<DtoClass>
{

    public Task<DtoClass> UpdateAsync(long id, DtoClass requestDto);

}
