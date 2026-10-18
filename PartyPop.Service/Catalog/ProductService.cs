using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PartyPop.Core.DTOs.Common;
using PartyPop.Core.DTOs.Products;
using PartyPop.Core.Entities;
using PartyPop.Core.Enums;
using PartyPop.Core.Exceptions;
using PartyPop.Core.Interfaces.Repositories;
using PartyPop.Core.Interfaces.Services;
using PartyPop.Core.Settings;

namespace PartyPop.Service.Catalog;

public class ProductService : IProductService
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _uow;
    private readonly IFileStorage _files;
    private readonly IAvailabilityCalculator _calculator;
    private readonly IMapper _mapper;
    private readonly TimeProvider _time;
    private readonly FileStorageSettings _fileSettings;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository products, ICategoryRepository categories, IUnitOfWork uow,
        IFileStorage files, IAvailabilityCalculator calculator, IMapper mapper, TimeProvider time,
        IOptions<FileStorageSettings> fileSettings, ILogger<ProductService> logger)
    {
        _products = products;
        _categories = categories;
        _uow = uow;
        _files = files;
        _calculator = calculator;
        _mapper = mapper;
        _time = time;
        _fileSettings = fileSettings.Value;
        _logger = logger;
    }

    // ================= קריאה (שלב 6) =================

    public async Task<PagedResult<ProductResponse>> GetCatalogAsync(ProductQueryParams query, CancellationToken ct)
    {
        // ולידציה עסקית: תאריך בלי משמרת לא אומר כלום
        if (query.Date is not null && query.Shift is null)
            throw new BusinessRuleException("כשבוחרים תאריך צריך לבחור גם משמרת");
        if (query.MinPrice is not null && query.MaxPrice is not null && query.MinPrice > query.MaxPrice)
            throw new BusinessRuleException("מחיר מינימום גדול ממחיר מקסימום");
        if (query.Date is { } date)
            EnsureFuture(date, query.Shift!.Value);

        var (items, totalCount) = await _products.GetCatalogAsync(query, _calculator.FullDayFactor, ct);

        return new PagedResult<ProductResponse>
        {
            Items = items.Select(i =>
            {
                var response = ToResponse(i.Product);
                // כשיש תאריך ומשמרת, כל מוצר מציג כמה יחידות פנויות
                if (query.Date is not null)
                    response.Available = _calculator.GetAvailable(
                        i.Product, i.MorningReserved, i.AfternoonReserved, query.Shift!.Value);
                return response;
            }).ToList(),
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

    // ================= ניהול (שלב 7) =================

    public async Task<ProductResponse> CreateAsync(ProductCreateRequest request, CancellationToken ct)
    {
        await EnsureCategoryExistsAsync(request.CategoryId, ct);

        var now = _time.GetUtcNow().UtcDateTime;
        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            CategoryId = request.CategoryId,
            PricePerShift = request.PricePerShift,
            TotalQuantity = request.TotalQuantity,
            OutOfServiceQuantity = 0,
            MinAge = request.MinAge,
            Status = ProductStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _products.AddAsync(product, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductId} created", product.Id);
        return await GetByIdAsync(product.Id, ct);   // כולל שם הקטגוריה
    }

    public async Task UpdateAsync(int id, ProductUpdateRequest request, CancellationToken ct)
    {
        var product = await GetForUpdateAsync(id, ct);

        if (product.CategoryId != request.CategoryId)
            await EnsureCategoryExistsAsync(request.CategoryId, ct);

        // אי אפשר להוריד את הכמות מתחת למספר היחידות שבתיקון
        if (request.TotalQuantity < product.OutOfServiceQuantity)
            throw new BusinessRuleException(
                $"הכמות הכוללת לא יכולה להיות קטנה ממספר היחידות בתיקון ({product.OutOfServiceQuantity})");

        product.Name = request.Name.Trim();
        product.Description = request.Description.Trim();
        product.CategoryId = request.CategoryId;
        product.PricePerShift = request.PricePerShift;
        product.TotalQuantity = request.TotalQuantity;
        product.MinAge = request.MinAge;
        product.UpdatedAt = _time.GetUtcNow().UtcDateTime;

        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Product {ProductId} updated", id);
    }

    public async Task SetOutOfServiceAsync(int id, OutOfServiceRequest request, CancellationToken ct)
    {
        var product = await GetForUpdateAsync(id, ct);

        if (request.OutOfServiceQuantity > product.TotalQuantity)
            throw new BusinessRuleException(
                $"מספר היחידות בתיקון לא יכול להיות גדול מהכמות הכוללת ({product.TotalQuantity})");

        // הזמנות קיימות לא מתבטלות. ההגבלה חלה רק על הזמנות חדשות (סעיף 7 בתוכנית)
        product.OutOfServiceQuantity = request.OutOfServiceQuantity;
        product.UpdatedAt = _time.GetUtcNow().UtcDateTime;

        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Product {ProductId} out-of-service set to {Quantity}", id, request.OutOfServiceQuantity);
    }

    /// <summary>Soft delete: המוצר נשאר במסד (יש לו הזמנות בהיסטוריה), רק יורד מהקטלוג.</summary>
    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var product = await GetForUpdateAsync(id, ct);
        if (product.Status == ProductStatus.Inactive)
            return;   // כבר מושבת: אין מה לעשות, עדיין 204

        product.Status = ProductStatus.Inactive;
        product.UpdatedAt = _time.GetUtcNow().UtcDateTime;

        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Product {ProductId} deactivated", id);
    }

    /// <summary>החזרת מוצר מושבת לקטלוג.</summary>
    public async Task ActivateAsync(int id, CancellationToken ct)
    {
        var product = await GetForUpdateAsync(id, ct);
        if (product.Status == ProductStatus.Active)
            return;

        product.Status = ProductStatus.Active;
        product.UpdatedAt = _time.GetUtcNow().UtcDateTime;

        await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("Product {ProductId} activated", id);
    }

    public async Task<string> UploadImageAsync(int id, FileUpload file, CancellationToken ct)
    {
        var product = await GetForUpdateAsync(id, ct);

        // ---- ולידציה של הקובץ ----
        if (file.Length == 0)
            throw new BusinessRuleException("הקובץ ריק");
        if (file.Length > _fileSettings.MaxFileSizeBytes)
            throw new BusinessRuleException($"הקובץ גדול מדי (עד {_fileSettings.MaxFileSizeBytes / 1024 / 1024}MB)");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_fileSettings.AllowedExtensions.Contains(extension))
            throw new BusinessRuleException("מותר להעלות רק jpg, png או webp");

        // לא סומכים על הסיומת בלבד: בודקים את הבתים הראשונים של הקובץ
        if (!await LooksLikeImageAsync(file.Content, ct))
            throw new BusinessRuleException("הקובץ אינו תמונה תקינה");

        // ---- שמירה ----
        var newUrl = await _files.SaveAsync(file.Content, extension, ct);
        var oldUrl = product.ImageUrl;

        product.ImageUrl = newUrl;
        product.UpdatedAt = _time.GetUtcNow().UtcDateTime;
        try
        {
            await _uow.SaveChangesAsync(ct);
        }
        catch
        {
            _files.Delete(newUrl);   // המסד לא התעדכן: לא משאירים קובץ יתום
            throw;
        }

        if (oldUrl is not null)
            _files.Delete(oldUrl);   // התמונה הקודמת כבר לא בשימוש

        _logger.LogInformation("Product {ProductId} image uploaded", id);
        return newUrl;
    }

    // ================= זמינות (שלב 8) =================

    public async Task<AvailabilityResponse> GetAvailabilityAsync(int id, DateOnly date, ShiftType shift, CancellationToken ct)
    {
        EnsureFuture(date, shift);

        // מוצר + המונים של התאריך בשאילתה אחת
        var row = await _products.GetWithUsageAsync(id, date, ct)
                  ?? throw new NotFoundException("המוצר לא נמצא");

        return new AvailabilityResponse
        {
            ProductId = id,
            Date = date,
            Shift = shift,
            Available = _calculator.GetAvailable(row.Product, row.MorningReserved, row.AfternoonReserved, shift),
            Price = _calculator.GetUnitPrice(row.Product.PricePerShift, shift)
        };
    }

    // ================= עזר =================

    private void EnsureFuture(DateOnly date, ShiftType shift)
    {
        if (_calculator.GetShiftStartUtc(date, shift) <= _time.GetUtcNow().UtcDateTime)
            throw new BusinessRuleException("התאריך והמשמרת שנבחרו כבר עברו");
    }

    private async Task<Product> GetForUpdateAsync(int id, CancellationToken ct) =>
        await _products.GetByIdAsync(id, ct) ?? throw new NotFoundException("המוצר לא נמצא");

    private async Task EnsureCategoryExistsAsync(int categoryId, CancellationToken ct)
    {
        if (await _categories.GetByIdAsync(categoryId, ct) is null)
            throw new BusinessRuleException("הקטגוריה שנבחרה לא קיימת");
    }

    private ProductResponse ToResponse(Product product)
    {
        var response = _mapper.Map<ProductResponse>(product);
        // המחיר ליום שלם לא נשמר במסד, אלא מחושב כאן
        response.FullDayPrice = _calculator.GetUnitPrice(product.PricePerShift, ShiftType.FullDay);
        return response;
    }

    /// <summary>"חתימה" של JPG / PNG / WEBP בתחילת הקובץ.</summary>
    private static async Task<bool> LooksLikeImageAsync(Stream stream, CancellationToken ct)
    {
        var header = new byte[12];
        var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, ct);
        stream.Position = 0;   // מחזירים להתחלה כדי לשמור את כל הקובץ
        if (read < 12) return false;

        var isJpg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        var isWebp = header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F'
                     && header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P';
        return isJpg || isPng || isWebp;
    }
}
