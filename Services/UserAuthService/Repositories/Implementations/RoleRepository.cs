using Microsoft.EntityFrameworkCore;
using UserAuthService.Data;
using UserAuthService.Entities;
using UserAuthService.Repositories.Interfaces;

namespace UserAuthService.Repositories.Implementations;

public class RoleRepository : IRoleRepository
{
    private readonly UsersDbContext _context;

    public RoleRepository(UsersDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByNameAsync(string name)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == name);
    }
}