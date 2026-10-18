using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartyPop.API.Extensions;
using PartyPop.Core.DTOs.Users;
using PartyPop.Core.Interfaces.Services;

namespace PartyPop.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IAuthService _auth;

    // ה-Controller מזריק רק Service, אף פעם לא DbContext
    public UsersController(IAuthService auth) => _auth = auth;

    /// <summary>הרשמה. תמיד יוצרת לקוח (Customer).</summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var user = await _auth.RegisterAsync(request, ct);
        return CreatedAtAction(nameof(GetMe), null, user);   // 201 + Location
    }

    /// <summary>פרטי המשתמש המחובר.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetMe(CancellationToken ct)
    {
        return Ok(await _auth.GetMeAsync(User.GetUserId(), ct));
    }
}
