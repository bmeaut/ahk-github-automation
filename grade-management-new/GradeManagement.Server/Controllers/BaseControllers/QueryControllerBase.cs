using GradeManagement.Bll.Services.BaseServices;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers.BaseControllers;

public abstract class QueryControllerBase<TDto> : ControllerBase
{
    private readonly IQueryServiceBase<TDto> _queryService;

    protected QueryControllerBase(IQueryServiceBase<TDto> queryService)
    {
        _queryService = queryService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IEnumerable<TDto>> GetAllAsync()
    {
        return await _queryService.GetAllAsync();
    }

    [HttpGet("{id:long}")]
    public async Task<TDto> GetByIdAsync([FromRoute] long id)
    {
        return await _queryService.GetByIdAsync(id);
    }

}
