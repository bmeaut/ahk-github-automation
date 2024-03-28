using GradeManagement.Bll.BaseServices;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers.BaseControllers;

public abstract class CrudControllerBase<TDto>(ICrudServiceBase<TDto> crudService)
    : RestrictedCrudControllerBase<TDto>(crudService)
{
    [HttpPut("{id:long}")]
    public async Task<TDto> UpdateAsync([FromRoute] long id, [FromBody] TDto requestDto)
    {
        return await crudService.UpdateAsync(id, requestDto);
    }
}
