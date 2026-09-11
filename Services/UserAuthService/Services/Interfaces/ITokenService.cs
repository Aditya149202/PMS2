using UserAuthService.DTOs;
using UserAuthService.Entities;
namespace UserAuthService.Services.Interfaces;
public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashToken(string rawToken);

}