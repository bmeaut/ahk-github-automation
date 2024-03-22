using GradeManagement.Bll.BaseServices;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers.BaseControllers;

public abstract class RestrictedControllerBase<TRequestDto, TResponseDto> : ControllerBase
{
    private readonly IRestrictedServiceBase<TRequestDto, TResponseDto> _restrictedService;

    protected RestrictedControllerBase(IRestrictedServiceBase<TRequestDto, TResponseDto> restrictedService)
    {
        _restrictedService = restrictedService;
    }

    [HttpGet]
    public async Task<IEnumerable<TResponseDto>> GetALlAsync()
    {
        return await _restrictedService.GetAllAsync();
    }

    [HttpGet("{id:long}")]
    public async Task<TResponseDto> GetByIdAsync([FromRoute] long id)
    {
        return await _restrictedService.GetByIdAsync(id);
    }

    [HttpPost]
    public async Task<TResponseDto> CreateAsync([FromBody] TRequestDto requestDto)
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
