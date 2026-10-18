using AutoMapper;
using Microsoft.Extensions.Logging;
using PartyPop.Core.DTOs.Categories;
using PartyPop.Core.Entities;
using PartyPop.Core.Exceptions;
using PartyPop.Core.Interfaces.Repositories;
using PartyPop.Core.Interfaces.Services;

namespace PartyPop.Service.Catalog;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(ICategoryRepository categories, IUnitOfWork uow,
        IMapper mapper, ILogger<CategoryService> logger)
    {
        _categories = categories;
        _uow = uow;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken ct) =>
        _mapper.Map<List<CategoryResponse>>(await _categories.GetAllAsync(ct));

    public async Task<CategoryResponse> CreateAsync(CategoryRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        if (await _categories.NameExistsAsync(name, null, ct))
            throw new ConflictException("כבר קיימת קטגוריה בשם הזה");

        var category = new Category { Name = name, Description = request.Description?.Trim() };
        await _categories.AddAsync(category, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Category {CategoryId} created", category.Id);
        return _mapper.Map<CategoryResponse>(category);
    }

    public async Task UpdateAsync(int id, CategoryRequest request, CancellationToken ct)
    {
        var category = await _categories.GetByIdAsync(id, ct)
                       ?? throw new NotFoundException("הקטגוריה לא נמצאה");

        var name = request.Name.Trim();
        if (await _categories.NameExistsAsync(name, id, ct))
            throw new ConflictException("כבר קיימת קטגוריה בשם הזה");

        category.Name = name;
        category.Description = request.Description?.Trim();
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Category {CategoryId} updated", id);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var category = await _categories.GetByIdAsync(id, ct)
                       ?? throw new NotFoundException("הקטגוריה לא נמצאה");

        if (await _categories.HasProductsAsync(id, ct))
            throw new ConflictException("לא ניתן למחוק קטגוריה שיש בה מוצרים");

        _categories.Remove(category);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Category {CategoryId} deleted", id);
    }
}
