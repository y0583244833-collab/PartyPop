using AutoMapper;
using PartyPop.Core.DTOs.Common;
using PartyPop.Core.DTOs.Products;
using PartyPop.Core.Entities;
using PartyPop.Core.Enums;
using PartyPop.Core.Exceptions;
using PartyPop.Core.Interfaces.Repositories;
using PartyPop.Core.Interfaces.Services;

namespace PartyPop.Service.Catalog;

public class ProductService : IProductService
{
    private readonly IProductRepository _products;
    private readonly IAvailabilityCalculator _calculator;
    private readonly IMapper _mapper;

    public ProductService(IProductRepository products, IAvailabilityCalculator calculator, IMapper mapper)
    {
        _products = products;
        _calculator = calculator;
        _mapper = mapper;
    }

    public async Task<PagedResult<ProductResponse>> GetCatalogAsync(ProductQueryParams query, CancellationToken ct)
    {
        // ולידציה עסקית: תאריך בלי משמרת לא אומר כלום
        if (query.Date is not null && query.Shift is null)
            throw new BusinessRuleException("כשבוחרים תאריך צריך לבחור גם משמרת");
        if (query.MinPrice is not null && query.MaxPrice is not null && query.MinPrice > query.MaxPrice)
            throw new BusinessRuleException("מחיר מינימום גדול ממחיר מקסימום");

        var (items, totalCount) = await _products.GetCatalogAsync(query, _calculator.FullDayFactor, ct);

        return new PagedResult<ProductResponse>
        {
            Items = items.Select(i => ToResponse(i.Product)).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProductResponse> GetByIdAsync(int id, CancellationToken ct)
    {
        var product = await _products.GetDetailsAsync(id, ct)
                      ?? throw new NotFoundException("המוצר לא נמצא");
        return ToResponse(product);
    }

    private ProductResponse ToResponse(Product product)
    {
        var response = _mapper.Map<ProductResponse>(product);
        // המחיר ליום שלם לא נשמר במסד, אלא מחושב כאן
        response.FullDayPrice = _calculator.GetUnitPrice(product.PricePerShift, ShiftType.FullDay);
        return response;
    }

    // ---- שלבים הבאים ----

    public Task<AvailabilityResponse> GetAvailabilityAsync(int id, DateOnly date, ShiftType shift, CancellationToken ct) =>
        throw new NotImplementedException("ייכתב בשלב 8 (זמינות)");

    public Task<ProductResponse> CreateAsync(ProductCreateRequest request, CancellationToken ct) =>
        throw new NotImplementedException("ייכתב בשלב 7 (ניהול מוצרים)");

    public Task UpdateAsync(int id, ProductUpdateRequest request, CancellationToken ct) =>
        throw new NotImplementedException("ייכתב בשלב 7 (ניהול מוצרים)");

    public Task SetOutOfServiceAsync(int id, OutOfServiceRequest request, CancellationToken ct) =>
        throw new NotImplementedException("ייכתב בשלב 7 (ניהול מוצרים)");

    public Task DeleteAsync(int id, CancellationToken ct) =>
        throw new NotImplementedException("ייכתב בשלב 7 (ניהול מוצרים)");

    public Task<string> UploadImageAsync(int id, FileUpload file, CancellationToken ct) =>
        throw new NotImplementedException("ייכתב בשלב 7 (ניהול מוצרים)");
}
