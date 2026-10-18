using Microsoft.EntityFrameworkCore;
using PartyPop.Core.Entities;
using PartyPop.Core.Interfaces.Repositories;

namespace PartyPop.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly PartyPopDbContext _db;

    public UserRepository(PartyPopDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct) =>
        _db.Users.AnyAsync(u => u.Email == email, ct);

    public async Task AddAsync(User user, CancellationToken ct) =>
        await _db.Users.AddAsync(user, ct);
}
