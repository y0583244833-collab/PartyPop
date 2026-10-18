using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartyPop.API.Extensions;
using PartyPop.Core.DTOs.Carts;
using PartyPop.Core.Interfaces.Services;

namespace PartyPop.API.Controllers;

/// <summary>הסל של הלקוח המחובר. ה-UserId תמיד מה-Token, אף פעם לא מה-URL.</summary>
[ApiController]
[Route("api/carts/me")]
[Authorize(Roles = "Customer")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class CartsController : ControllerBase
{
    private readonly ICartService _carts;

    public CartsController(ICartService carts) => _carts = carts;

    /// <summary>הסל, כולל מחיר משוער ואזהרות זמינות.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CartResponse>> Get(CancellationToken ct) =>
        Ok(await _carts.GetMyCartAsync(User.GetUserId(), ct));

    /// <summary>קביעת התאריך והמשמרת של הסל.</summary>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetDateAndShift([FromBody] SetCartRequest request, CancellationToken ct)
    {
        await _carts.SetDateAndShiftAsync(User.GetUserId(), request, ct);
        return NoContent();
    }

    /// <summary>הוספת מוצר. מוצר שכבר בסל: הכמות גדלה.</summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CartItemResponse>> AddItem([FromBody] AddCartItemRequest request, CancellationToken ct)
    {
        var item = await _carts.AddItemAsync(User.GetUserId(), request, ct);
        return CreatedAtAction(nameof(Get), null, item);
    }

    /// <summary>שינוי כמות.</summary>
    [HttpPut("items/{itemId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateItem([FromRoute] int itemId, [FromBody] UpdateCartItemRequest request, CancellationToken ct)
    {
        await _carts.UpdateItemAsync(User.GetUserId(), itemId, request, ct);
        return NoContent();
    }

    /// <summary>הסרת פריט.</summary>
    [HttpDelete("items/{itemId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveItem([FromRoute] int itemId, CancellationToken ct)
    {
        await _carts.RemoveItemAsync(User.GetUserId(), itemId, ct);
        return NoContent();
    }
}
