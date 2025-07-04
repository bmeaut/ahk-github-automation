using Ahk.GradeManagement.Bll.Services.BaseServices;

using Microsoft.AspNetCore.Mvc;

namespace Ahk.GradeManagement.Api.Controllers.BaseControllers;

public abstract class CrudControllerBase<TDto>(ICrudServiceBase<TDto> crudService)
    : RestrictedCrudControllerBase<TDto>(crudService)
{
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public virtual async Task<TDto> UpdateAsync([FromRoute] long id, [FromBody] TDto requestDto)
    {
        return await crudService.UpdateAsync(id, requestDto);
    }
}
