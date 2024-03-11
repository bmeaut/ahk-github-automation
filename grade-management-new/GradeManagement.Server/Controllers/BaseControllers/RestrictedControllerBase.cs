using GradeManagement.Bll.BaseServices;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers.BaseControllers;

public abstract class RestrictedControllerBase<DtoClass> : ControllerBase
{
    private readonly IRestrictedServiceBase<DtoClass> _restrictedService;

    protected RestrictedControllerBase(IRestrictedServiceBase<DtoClass> restrictedService)
    {
        _restrictedService = restrictedService;
    }

    [HttpGet]
    public async Task<IEnumerable<DtoClass>> GetALlAsync()
    {
        return await _restrictedService.GetAllAsync();
    }

    [HttpGet("{id:long}")]
    public async Task<DtoClass> GetByIdAsync([FromRoute] long id)
    {
        return await _restrictedService.GetByIdAsync(id);
    }

    [HttpPost]
    public async Task<DtoClass> CreateAsync([FromBody] DtoClass requestDto)
    {
        return await _restrictedService.CreateAsync(requestDto);
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult> DeleteAsync([FromRoute] long id)
    {
        await _restrictedService.DeleteAsync(id);
        return NoContent();
    }
}
