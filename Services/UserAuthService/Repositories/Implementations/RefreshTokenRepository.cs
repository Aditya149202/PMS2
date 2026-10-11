using Microsoft.EntityFrameworkCore;
using UserAuthService.Data;
using UserAuthService.Entities;
using UserAuthService.Repositories.Interfaces;
namespace UserAuthService.Repositories.Implementations;
public class RefreshTokenRepository:IRefreshTokenRepository
{
    private readonly UsersDbContext _context;

    public RefreshTokenRepository(UsersDbContext context)
    {
        _context=context;
    }

    public async Task AddAsync(RefreshToken refreshToken)
    {
        await _context.RefreshTokens.AddAsync(refreshToken);
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
    {
        return await _context.RefreshTokens.Include(r=>r.User).ThenInclude(u=>u.Role).FirstOrDefaultAsync(r=>r.TokenHash==tokenHash);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAllForUserAsync(int userId)
    {
        var tokens=await _context.RefreshTokens.Where(r=>r.UserId==userId && r.RevokedAt==null).ToListAsync();
        foreach(var token in tokens)
        {
            token.RevokedAt=DateTime.UtcNow;
        }
    }
}