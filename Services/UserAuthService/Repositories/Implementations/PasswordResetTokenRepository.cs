using Microsoft.EntityFrameworkCore;
using UserAuthService.Data;
using UserAuthService.Entities;
using UserAuthService.Repositories.Interfaces;

namespace UserAuthService.Repositories.Implementations;
public class PasswordResetTokenRepository:IPasswordResetTokenRepository
{
    private readonly UsersDbContext _context;

    public PasswordResetTokenRepository(UsersDbContext context)
    {
        _context=context;
    }

    public async Task AddAsync(PasswordResetToken passwordResetToken)
    {
        await _context.PasswordResetTokens.AddAsync(passwordResetToken);
    }

    public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash)
    {
        return await _context.PasswordResetTokens.Include(r=>r.User).ThenInclude(u=>u.Role).FirstOrDefaultAsync(r=>r.TokenHash==tokenHash);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}