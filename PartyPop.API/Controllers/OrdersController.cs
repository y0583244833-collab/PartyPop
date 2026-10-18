using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartyPop.API.Extensions;
using PartyPop.Core.DTOs.Orders;
using PartyPop.Core.Interfaces.Services;

namespace PartyPop.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) => _orders = orders;

    /// <summary>אישור הזמנה מהסל. הכל או כלום.</summary>
    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]   // סל ריק / בלי תאריך / תאריך עבר
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]     // לא זמין / התנגשות concurrency
    public async Task<ActionResult<OrderResponse>> Checkout([FromBody] CheckoutRequest request, CancellationToken ct)
    {
        var order = await _orders.CheckoutAsync(User.GetUserId(), request, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);   // 201 + Location
    }

    /// <summary>ההזמנות שלי.</summary>
    [HttpGet("me")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetMine(CancellationToken ct) =>
        Ok(await _orders.GetMyOrdersAsync(User.GetUserId(), ct));

    /// <summary>הזמנה בודדת: הבעלים או מנהל.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById([FromRoute] int id, CancellationToken ct) =>
        Ok(await _orders.GetByIdAsync(id, User.GetUserId(), User.GetRole(), ct));

    /// <summary>לקוח: ביטול בלבד, עד 48 שעות לפני. מנהל: Cancelled או Completed בכל זמן.</summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus([FromRoute] int id, [FromBody] UpdateOrderStatusRequest request, CancellationToken ct)
    {
        await _orders.UpdateStatusAsync(id, User.GetUserId(), User.GetRole(), request, ct);
        return NoContent();
    }
}
