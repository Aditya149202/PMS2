using Microsoft.EntityFrameworkCore;
using UserAuthService.Data;
using UserAuthService.Entities;
using UserAuthService.Repositories.Interfaces;

namespace UserAuthService.Repositories.Implementations;

public class UserRepository : IUserRepository
{
    private readonly UsersDbContext _context;

    public UserRepository(UsersDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.Include(u=>u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.Include(u=>u.Role)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}