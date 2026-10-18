using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartyPop.Core.DTOs.Common;
using PartyPop.Core.DTOs.Products;
using PartyPop.Core.Interfaces.Services;

namespace PartyPop.API.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _products;

    public ProductsController(IProductService products) => _products = products;

    /// <summary>קטלוג: סינון, מיון ודפדוף.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ProductResponse>>> GetCatalog(
        [FromQuery] ProductQueryParams query, CancellationToken ct) =>
        Ok(await _products.GetCatalogAsync(query, ct));

    /// <summary>מוצר בודד.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetById([FromRoute] int id, CancellationToken ct) =>
        Ok(await _products.GetByIdAsync(id, ct));
}
