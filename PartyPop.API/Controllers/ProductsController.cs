using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PartyPop.Core.DTOs.Common;
using PartyPop.Core.DTOs.Products;
using PartyPop.Core.Enums;
using PartyPop.Core.Interfaces.Services;

namespace PartyPop.API.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _products;

    public ProductsController(IProductService products) => _products = products;

    // ================= לכולם =================

    /// <summary>קטלוג: סינון, מיון ודפדוף.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ProductResponse>>> GetCatalog(
        [FromQuery] ProductQueryParams query, CancellationToken ct)
    {
        // רק מנהל רואה מוצרים מושבתים
        if (!User.IsInRole("Admin"))
            query.IncludeInactive = false;
        return Ok(await _products.GetCatalogAsync(query, ct));
    }

    /// <summary>מוצר בודד.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetById([FromRoute] int id, CancellationToken ct) =>
        Ok(await _products.GetByIdAsync(id, ct));

    /// <summary>כמה יחידות פנויות בתאריך ובמשמרת, ומה המחיר.</summary>
    [HttpGet("{id:int}/availability")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AvailabilityResponse>> GetAvailability([FromRoute] int id,
        [FromQuery, BindRequired] DateOnly date, [FromQuery, BindRequired] ShiftType shift, CancellationToken ct) =>
        Ok(await _products.GetAvailabilityAsync(id, date, shift, ct));

    // ================= מנהל =================

    /// <summary>יצירת מוצר.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductResponse>> Create([FromBody] ProductCreateRequest request, CancellationToken ct)
    {
        var product = await _products.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);   // 201 + Location
    }

    /// <summary>עדכון מוצר.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] ProductUpdateRequest request, CancellationToken ct)
    {
        await _products.UpdateAsync(id, request, ct);
        return NoContent();
    }

    /// <summary>כמה יחידות בתיקון. הזמינות יורדת בהתאם.</summary>
    [HttpPatch("{id:int}/out-of-service")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetOutOfService([FromRoute] int id, [FromBody] OutOfServiceRequest request, CancellationToken ct)
    {
        await _products.SetOutOfServiceAsync(id, request, ct);
        return NoContent();
    }

    /// <summary>Soft delete: המוצר הופך ל-Inactive ויורד מהקטלוג.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        await _products.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>החזרת מוצר מושבת לקטלוג.</summary>
    [HttpPatch("{id:int}/activate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate([FromRoute] int id, CancellationToken ct)
    {
        await _products.ActivateAsync(id, ct);
        return NoContent();
    }

    /// <summary>העלאת תמונה (jpg / png / webp, עד 5MB).</summary>
    [HttpPost("{id:int}/image")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]   // קצת מעל 5MB: ההודעה המדויקת מגיעה מה-Service
    [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImageUploadResponse>> UploadImage([FromRoute] int id, IFormFile file, CancellationToken ct)
    {
        // IFormFile שייך ל-ASP.NET, ולכן לא עובר ל-Service. ממירים ל-FileUpload של Core
        await using var stream = file.OpenReadStream();
        var url = await _products.UploadImageAsync(id,
            new FileUpload(stream, file.FileName, file.ContentType, file.Length), ct);
        return Ok(new ImageUploadResponse(url));
    }
}
