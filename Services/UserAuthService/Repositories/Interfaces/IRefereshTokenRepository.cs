using UserAuthService.Entities;

namespace UserAuthService.Repositories.Interfaces;


public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken);

    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);

    Task SaveChangesAsync();
}