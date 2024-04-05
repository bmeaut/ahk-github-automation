using GradeManagement.Bll.BaseServices;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers.BaseControllers;

public abstract class RestrictedCrudControllerBase<TDto> : ControllerBase
{
    private readonly IRestrictedCrudServiceBase<TDto> _restrictedCrudService;

    protected RestrictedCrudControllerBase(IRestrictedCrudServiceBase<TDto> restrictedCrudService)
    {
        _restrictedCrudService = restrictedCrudService;
    }

    [HttpGet]
    public async Task<IEnumerable<TDto>> GetALlAsync()
    {
        return await _restrictedCrudService.GetAllAsync();
    }

    [HttpGet("{id:long}")]
    public async Task<TDto> GetByIdAsync([FromRoute] long id)
    {
        return await _restrictedCrudService.GetByIdAsync(id);
    }

    [HttpPost]
    public async Task<TDto> CreateAsync([FromBody] TDto requestDto)
    {
        return await _restrictedCrudService.CreateAsync(requestDto);
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult> DeleteAsync([FromRoute] long id)
    {
        await _restrictedCrudService.DeleteAsync(id);
        return NoContent();
    }
}
