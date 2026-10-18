using PartyPop.Core.DTOs.Categories;

namespace PartyPop.Core.Interfaces.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken ct);
    Task<CategoryResponse> CreateAsync(CategoryRequest request, CancellationToken ct);
    Task UpdateAsync(int id, CategoryRequest request, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}
