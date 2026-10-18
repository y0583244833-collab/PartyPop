using AutoMapper;
using PartyPop.Core.DTOs.Categories;
using PartyPop.Core.DTOs.Products;
using PartyPop.Core.Entities;

namespace PartyPop.Service.Mapping;

public class CatalogProfile : Profile
{
    public CatalogProfile()
    {
        CreateMap<Category, CategoryResponse>();

        CreateMap<Product, ProductResponse>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            // מחושבים ב-Service, לא מגיעים מהישות
            .ForMember(d => d.FullDayPrice, o => o.Ignore())
            .ForMember(d => d.Available, o => o.Ignore());
    }
}
