using UserAuthService.Entities;

namespace UserAuthService.Repositories.Interfaces;


public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token);

    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash);

    Task SaveChangesAsync();
}