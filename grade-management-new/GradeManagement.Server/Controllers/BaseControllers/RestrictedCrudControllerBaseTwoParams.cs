using GradeManagement.Bll.Services.BaseServices;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers.BaseControllers;

public abstract class RestrictedCrudControllerBase<TRequestDto, TResponseDto>(
    IRestrictedCrudServiceBase<TRequestDto, TResponseDto> restrictedCrudService)
    : QueryControllerBase<TResponseDto>(restrictedCrudService)
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public virtual async Task<TResponseDto> CreateAsync([FromBody] TRequestDto requestDto)
    {
        return await restrictedCrudService.CreateAsync(requestDto);
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public virtual async Task<ActionResult> DeleteAsync([FromRoute] long id)
    {
        await restrictedCrudService.DeleteAsync(id);
        return NoContent();
    }
}
