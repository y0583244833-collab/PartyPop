using AutoMapper;
using Microsoft.Extensions.Logging;
using PartyPop.Core.DTOs.Users;
using PartyPop.Core.Entities;
using PartyPop.Core.Enums;
using PartyPop.Core.Exceptions;
using PartyPop.Core.Interfaces.Repositories;
using PartyPop.Core.Interfaces.Services;

namespace PartyPop.Service.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IMapper _mapper;
    private readonly TimeProvider _time;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUserRepository users, IUnitOfWork unitOfWork, IPasswordHasher hasher,
        ITokenService tokens, IMapper mapper, TimeProvider time, ILogger<AuthService> logger)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _hasher = hasher;
        _tokens = tokens;
        _mapper = mapper;
        _time = time;
        _logger = logger;
    }

    public async Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);

        // ולידציה עסקית (תלויה במצב המסד) – ב-Service, לא ב-Controller
        if (await _users.EmailExistsAsync(email, ct))
            throw new ConflictException("האימייל כבר רשום במערכת");

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Phone = request.Phone.Trim(),
            PasswordHash = _hasher.Hash(request.Password),
            Role = UserRole.Customer,               // הרשמה תמיד יוצרת לקוח
            CreatedAt = _time.GetUtcNow().UtcDateTime
        };

        await _users.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} registered", user.Id);   // רק UserId, בלי אימייל וסיסמה
        return _mapper.Map<UserResponse>(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(NormalizeEmail(request.Email), ct);

        // אותה הודעה לאימייל לא קיים ולסיסמה שגויה, כדי לא לחשוף אילו אימיילים רשומים
        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed");   // בלי האימייל שהוקלד ובלי הסיסמה
            throw new UnauthorizedException();
        }

        var (token, expiresAt) = _tokens.CreateToken(user);
        _logger.LogInformation("User {UserId} logged in", user.Id);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = _mapper.Map<UserResponse>(user)
        };
    }

    public async Task<UserResponse> GetMeAsync(int userId, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("המשתמש לא נמצא");
        return _mapper.Map<UserResponse>(user);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
