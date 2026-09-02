using System.Security.Claims;
using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Auth.Dto;
using Ahk.Web.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ahk.Web.Server.Auth;

/// <summary>
/// The caller's own access tokens: mint one, list them, revoke one. What a token is *for* is documented on
/// <see cref="Ahk.Web.Data.Entities.PersonalAccessToken"/>.
///
/// <para>⚠️ Cookie-only, by construction: a bare <c>[Authorize]</c> uses the default authenticate scheme, and
/// only the course read endpoints opt into <see cref="AuthSchemes.CookieOrPersonalToken"/>. So a token can
/// never mint or revoke another token — reaching this controller takes an interactive sign-in.</para>
///
/// <para>The owner always comes from the signed-in principal. Nothing here takes a user id from the client.</para>
/// </summary>
[ApiController]
[Route("api/profile/tokens")]
[Authorize]
public sealed class PersonalAccessTokensController : ControllerBase
{
    private readonly IPersonalAccessTokenService tokens;

    public PersonalAccessTokensController(IPersonalAccessTokenService tokens) => this.tokens = tokens;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PersonalAccessTokenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PersonalAccessTokenDto>>> List(CancellationToken cancellationToken)
    {
        var mine = await tokens.ListForUserAsync(CurrentUserId(), cancellationToken);
        return Ok(mine.Select(ToDto).ToList());
    }

    [HttpPost]
    [ProducesResponseType(typeof(PersonalAccessTokenDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PersonalAccessTokenDto>> Create(
        [FromBody] CreatePersonalAccessTokenRequest request,
        CancellationToken cancellationToken)
    {
        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        var token = await tokens.CreateAsync(CurrentUserId(), description, cancellationToken);

        return CreatedAtAction(nameof(List), ToDto(token));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(int id, CancellationToken cancellationToken) =>
        await tokens.RevokeAsync(CurrentUserId(), id, cancellationToken) ? NoContent() : NotFound();

    private int CurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);

    private static PersonalAccessTokenDto ToDto(PersonalAccessToken token) => new()
    {
        Id = token.Id,
        Token = token.Token,
        Description = token.Description,
        CreatedAt = token.CreatedAt,
        LastUsedAt = token.LastUsedAt,
        RevokedAt = token.RevokedAt,
    };
}
