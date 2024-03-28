using GradeManagement.Bll.BaseServices;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers.BaseControllers;

public abstract class CrudControllerBase<TRequestDto, TResponseDto>(ICrudServiceBase<TRequestDto, TResponseDto> crudService)
    : RestrictedCrudControllerBase<TRequestDto, TResponseDto>(crudService)
{
    [HttpPut("{id:long}")]
    public async Task<TResponseDto> UpdateAsync([FromRoute] long id, [FromBody] TRequestDto requestDto)
    {
        return await crudService.UpdateAsync(id, requestDto);
    }
}
