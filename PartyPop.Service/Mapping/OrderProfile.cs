using AutoMapper;
using PartyPop.Core.DTOs.Orders;
using PartyPop.Core.Entities;

namespace PartyPop.Service.Mapping;

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderResponse>()
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.User.FirstName + " " + s.User.LastName))
            .ForMember(d => d.Shift, o => o.MapFrom(s => s.Shift.ToString()))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

        CreateMap<OrderItem, OrderItemResponse>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name));
    }
}
