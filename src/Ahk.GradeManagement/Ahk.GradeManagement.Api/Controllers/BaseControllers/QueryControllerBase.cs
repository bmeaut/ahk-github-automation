using Ahk.GradeManagement.Bll.Services.BaseServices;

using Microsoft.AspNetCore.Mvc;

namespace Ahk.GradeManagement.Api.Controllers.BaseControllers;

public abstract class QueryControllerBase<TDto>(IQueryServiceBase<TDto> queryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public virtual async Task<IEnumerable<TDto>> GetAllAsync()
    {
        return await queryService.GetAllAsync();
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public virtual async Task<TDto> GetByIdAsync([FromRoute] long id)
    {
        return await queryService.GetByIdAsync(id);
    }
}
