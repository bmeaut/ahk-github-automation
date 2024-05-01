using GradeManagement.Bll.BaseServices;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers.BaseControllers;

public abstract class RestrictedCrudControllerBase<TDto>(IRestrictedCrudServiceBase<TDto> restrictedCrudService)
    : QueryControllerBase<TDto>(restrictedCrudService)
{
    [HttpPost]
    public async Task<TDto> CreateAsync([FromBody] TDto requestDto)
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
