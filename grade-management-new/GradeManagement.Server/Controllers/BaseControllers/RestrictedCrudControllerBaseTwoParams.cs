using GradeManagement.Bll.BaseServices;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers.BaseControllers;

public abstract class RestrictedCrudControllerBase<TRequestDto, TResponseDto>(
    IRestrictedCrudServiceBase<TRequestDto, TResponseDto> restrictedCrudService)
    : QueryControllerBase<TResponseDto>(restrictedCrudService)
{
    [HttpPost]
    public virtual async Task<TResponseDto> CreateAsync([FromBody] TRequestDto requestDto)
    {
        return await restrictedCrudService.CreateAsync(requestDto);
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult> DeleteAsync([FromRoute] long id)
    {
        await restrictedCrudService.DeleteAsync(id);
        return NoContent();
    }
}
