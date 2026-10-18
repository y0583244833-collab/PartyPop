using AutoMapper;
using PartyPop.Core.DTOs.Users;
using PartyPop.Core.Entities;

namespace PartyPop.Service.Mapping;

public class UserProfile : Profile
{
    public UserProfile()
    {
        // PasswordHash לא קיים ב-UserResponse, ולכן לא יוצא החוצה
        CreateMap<User, UserResponse>()
            .ForMember(d => d.Role, o => o.MapFrom(s => s.Role.ToString()));
    }
}
