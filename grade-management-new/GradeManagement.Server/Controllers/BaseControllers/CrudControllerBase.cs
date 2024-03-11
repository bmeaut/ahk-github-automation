using GradeManagement.Bll.BaseServices;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers.BaseControllers;

public abstract class CrudControllerBase<DtoClass>(ICrudServiceBase<DtoClass> crudService)
    : RestrictedControllerBase<DtoClass>(crudService)
{
    [HttpPut("{id:long}")]
    public async Task<DtoClass> UpdateAsync([FromRoute] long id, [FromBody] DtoClass requestDto)
    {
        return await crudService.UpdateAsync(id, requestDto);
    }
}
