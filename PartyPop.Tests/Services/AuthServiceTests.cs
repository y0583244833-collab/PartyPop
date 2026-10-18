using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using PartyPop.Core.DTOs.Users;
using PartyPop.Core.Entities;
using PartyPop.Core.Exceptions;
using PartyPop.Core.Interfaces.Repositories;
using PartyPop.Core.Interfaces.Services;
using PartyPop.Service.Auth;
using PartyPop.Tests.Helpers;

namespace PartyPop.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokens = new();

    private AuthService CreateService() => new(_users.Object, _uow.Object, _hasher.Object, _tokens.Object,
        TestFactory.Mapper(), new FakeTimeProvider(), Mock.Of<ILogger<AuthService>>());

    [Fact]
    public async Task Register_ExistingEmail_ThrowsConflict()
    {
        _users.Setup(u => u.EmailExistsAsync("a@b.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => CreateService().RegisterAsync(
            new RegisterRequest { FirstName = "A", LastName = "B", Email = "a@b.com", Password = "123456", Phone = "0521234567" },
            CancellationToken.None));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorized()
    {
        _users.Setup(u => u.GetByEmailAsync("a@b.com", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new User { Id = 1, Email = "a@b.com", PasswordHash = "hash" });
        _hasher.Setup(h => h.Verify("wrong", "hash")).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedException>(() => CreateService().LoginAsync(
            new LoginRequest { Email = "a@b.com", Password = "wrong" }, CancellationToken.None));

        _tokens.Verify(t => t.CreateToken(It.IsAny<User>()), Times.Never);
    }
}
