using PartyPop.Core.DTOs.Common;
using PartyPop.Core.DTOs.Products;
using PartyPop.Core.Enums;

namespace PartyPop.Core.Interfaces.Services;

public interface IProductService
{
    Task<PagedResult<ProductResponse>> GetCatalogAsync(ProductQueryParams query, CancellationToken ct);
    Task<ProductResponse> GetByIdAsync(int id, CancellationToken ct);
    Task<AvailabilityResponse> GetAvailabilityAsync(int id, DateOnly date, ShiftType shift, CancellationToken ct);

    Task<ProductResponse> CreateAsync(ProductCreateRequest request, CancellationToken ct);
    Task UpdateAsync(int id, ProductUpdateRequest request, CancellationToken ct);
    Task SetOutOfServiceAsync(int id, OutOfServiceRequest request, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
    Task ActivateAsync(int id, CancellationToken ct);
    Task<string> UploadImageAsync(int id, FileUpload file, CancellationToken ct);
}
