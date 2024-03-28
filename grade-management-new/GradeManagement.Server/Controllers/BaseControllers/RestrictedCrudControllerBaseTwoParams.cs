using GradeManagement.Bll.BaseServices;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers.BaseControllers;

public abstract class RestrictedCrudControllerBase<TRequestDto, TResponseDto> : ControllerBase
{
    private readonly IRestrictedCrudServiceBase<TRequestDto, TResponseDto> _restrictedCrudService;

    protected RestrictedCrudControllerBase(IRestrictedCrudServiceBase<TRequestDto, TResponseDto> restrictedCrudService)
    {
        _restrictedCrudService = restrictedCrudService;
    }

    [HttpGet]
    public async Task<IEnumerable<TResponseDto>> GetALlAsync()
    {
        return await _restrictedCrudService.GetAllAsync();
    }

    [HttpGet("{id:long}")]
    public async Task<TResponseDto> GetByIdAsync([FromRoute] long id)
    {
        return await _restrictedCrudService.GetByIdAsync(id);
    }

    [HttpPost]
    public async Task<TResponseDto> CreateAsync([FromBody] TRequestDto requestDto)
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
