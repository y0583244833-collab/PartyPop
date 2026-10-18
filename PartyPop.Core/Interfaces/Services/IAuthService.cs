using PartyPop.Core.DTOs.Users;

namespace PartyPop.Core.Interfaces.Services;

public interface IAuthService
{
    Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<UserResponse> GetMeAsync(int userId, CancellationToken ct);
}
