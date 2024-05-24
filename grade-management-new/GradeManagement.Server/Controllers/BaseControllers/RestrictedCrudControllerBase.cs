using GradeManagement.Bll.Services.BaseServices;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers.BaseControllers;

public abstract class RestrictedCrudControllerBase<TDto>(IRestrictedCrudServiceBase<TDto> restrictedCrudService)
    : QueryControllerBase<TDto>(restrictedCrudService)
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<TDto> CreateAsync([FromBody] TDto requestDto)
    {
        return await restrictedCrudService.CreateAsync(requestDto);
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> DeleteAsync([FromRoute] long id)
    {
        await restrictedCrudService.DeleteAsync(id);
        return NoContent();
    }
}
