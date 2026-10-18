using PartyPop.Core.Entities;

namespace PartyPop.Core.Interfaces.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(User user);
}
