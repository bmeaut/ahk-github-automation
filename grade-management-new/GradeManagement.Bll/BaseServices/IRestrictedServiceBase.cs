namespace GradeManagement.Bll.BaseServices;

public interface IRestrictedServiceBase<DtoClass>
{
    public Task<IEnumerable<DtoClass>> GetAllAsync();

    public Task<DtoClass> GetByIdAsync(long id);

    public Task<DtoClass> CreateAsync(DtoClass requestDto);

    public Task DeleteAsync(long id);
}
